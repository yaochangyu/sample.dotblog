# [.NET] ASP.NET Core 10 微服務可觀測性實戰 - OpenTelemetry + Serilog + Jaeger + Aspire Dashboard

在微服務架構中，一個使用者請求可能跨越多個服務，當問題發生時，如何追蹤這個請求到底經過了哪些服務？每個服務做了什麼事？花了多少時間？這就是「可觀測性（Observability）」要解決的問題。

本篇文章將介紹如何在 ASP.NET Core 10 微服務中整合 OpenTelemetry、Serilog、Jaeger 與 Aspire Dashboard，建立完整的分散式追蹤與結構化日誌方案。

## 開發環境

- .NET 10
- ASP.NET Core 10
- OpenTelemetry .NET SDK 1.15.0
- Serilog 10.0.0
- Serilog.Sinks.OpenTelemetry 4.2.0
- Docker Compose v3.8
- Jaeger (All-in-One)
- Seq
- .NET Aspire Dashboard
- OpenTelemetry Collector Contrib

---

## 架構概覽

本篇範例的服務架構如下：

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Nuxt 4     │────▶│   backend-a  │────▶│   backend-b  │
│  (Frontend)  │     │ ASP.NET 10   │     │ ASP.NET 10   │
└──────────────┘     └──────┬───────┘     └──────┬───────┘
                            │                    │
                     Serilog│+ OTel SDK   Serilog│+ OTel SDK
                            │                    │
                            ▼                    ▼
                    ┌───────────────────────────────┐
                    │      OTel Collector (OTLP)    │
                    └───┬──────────┬────────────┬───┘
                        │          │            │
                        ▼          ▼            ▼
                    ┌───────┐ ┌─────────┐ ┌─────────────┐
                    │Jaeger │ │  Seq    │ │   Aspire    │
                    │Traces │ │  Logs   │ │  Dashboard  │
                    └───────┘ └─────────┘ └─────────────┘
```

三個核心服務：

| 服務名稱 | 技術棧 | 本機端口 | 功能 |
|---------|-------|--------|------|
| **frontend** | Nuxt 4 + Vue 3 | 3000 | 前端 UI |
| **backend-a** | ASP.NET Core 10 | 5100 | API 閘道，呼叫 backend-b |
| **backend-b** | ASP.NET Core 10 | 5200 | 核心服務，產生天氣資料 |

四個基礎設施：

| 服務名稱 | 本機端口 | 用途 |
|---------|--------|------|
| **otel-collector** | 4317 (gRPC), 4318 (HTTP) | 接收、轉發遙測資料 |
| **jaeger** | 16686 | 分散式追蹤 UI |
| **seq** | 5341 | 結構化日誌 UI |
| **aspire-dashboard** | 18888 | 整合 Traces / Metrics / Logs 儀表板 |

---

## 微服務中的系統追蹤

### 為什麼需要分散式追蹤？

單體應用（Monolith）的錯誤排查相對單純，從日誌就能看到呼叫堆疊。但微服務架構下，一個請求可能經過 Frontend → Backend-A → Backend-B，每個服務各自產生日誌，要把它們串起來就變得困難。

分散式追蹤的核心概念是：**用一個唯一的 Trace ID 將所有相關的操作串在一起**。

### 可觀測性的三根支柱

OpenTelemetry 定義了三種訊號（Signal）：

| 訊號類型 | 說明 | .NET 對應 API |
|---------|------|--------------|
| **Traces** | 追蹤請求在多個服務間的流程 | `System.Diagnostics.Activity` |
| **Metrics** | 衡量系統效能指標（如請求數、延遲） | `System.Diagnostics.Metrics.Meter` |
| **Logs** | 記錄離散事件 | `Microsoft.Extensions.Logging.ILogger` |

### W3C Trace Context 標準

OpenTelemetry 使用 W3C Trace Context 標準進行跨服務的追蹤上下文傳播。核心是 `traceparent` header：

```
traceparent: 00-{trace-id}-{parent-id}-{trace-flags}
```

- `trace-id`：32 字元的 hex 字串，整條追蹤鏈共用同一個
- `parent-id`：16 字元的 hex 字串，代表父 Span 的 ID
- `trace-flags`：取樣旗標，`01` 表示已取樣

ASP.NET Core 的 OpenTelemetry Instrumentation 會自動讀取 `traceparent` header、建立 child span，並在發出 HttpClient 請求時自動注入 `traceparent`，不需要手動處理。

### 本專案的追蹤鏈路

```
Browser (OTel fetch span, 注入 traceparent)
  └──▶ Nuxt Server (server span, 從 request headers 提取 trace context)
         └──▶ Nuxt Server (client span, fetchWithTracing 建立, 注入 traceparent)
                └──▶ Backend-A (讀取 traceparent, 建立 child span)
                       └──▶ Backend-B (讀取 traceparent, 建立 child span)
```

在 Jaeger UI 中會呈現完整的追蹤瀑布圖：

```
Jaeger Trace (單一 Trace ID)
├─ Span: frontend.fetch /api/weather (browser client span)
├─ Span: frontend-server.GET /api/weather (server span)
│   └─ Span: frontend-server.GET http://backend-a:8080/Weather (client span)
│       └─ Span: backend-a.GET /Weather
│           └─ Span: backend-a.HttpClient → backend-b
│               └─ Span: backend-b.GET /Weather
```

---

## OpenTelemetry 的設定方式

### NuGet 套件

後端服務需要安裝以下套件：

```xml
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
```

| 套件 | 用途 |
|------|------|
| `OpenTelemetry.Extensions.Hosting` | 與 ASP.NET Core DI 整合，管理 Provider 生命週期 |
| `OpenTelemetry.Instrumentation.AspNetCore` | 自動追蹤 ASP.NET Core 請求 |
| `OpenTelemetry.Instrumentation.Http` | 自動追蹤 HttpClient 呼叫 |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 透過 OTLP 協定匯出遙測資料 |

### 設定 OpenTelemetry（Program.cs）

```csharp
var otlpGrpcEndpoint = Environment.GetEnvironmentVariable("OTLP_GRPC_ENDPOINT")
    ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("backend-a"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter("otlp-grpc", options =>
        {
            options.Endpoint = new Uri(otlpGrpcEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter("otlp-grpc", options =>
        {
            options.Endpoint = new Uri(otlpGrpcEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        }));
```

**重點說明**：

- `ConfigureResource`：設定服務名稱，會顯示在 Jaeger 和 Aspire Dashboard 上
- `AddAspNetCoreInstrumentation`：自動為所有 HTTP 請求建立 Span
- `AddHttpClientInstrumentation`：自動為所有 HttpClient 呼叫建立 child Span，並注入 `traceparent` header
- `AddOtlpExporter`：將遙測資料透過 OTLP gRPC 發送到 OTel Collector

### OpenTelemetry Collector 配置

OTel Collector 負責接收遙測資料，再轉發到各個後端（Jaeger、Aspire Dashboard）。

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318
        cors:
          allowed_origins:
            - "http://localhost:3000"
          allowed_headers:
            - "*"

exporters:
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true

  otlp/aspire:
    endpoint: aspire-dashboard:18889
    tls:
      insecure: true

processors:
  batch:

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/jaeger, otlp/aspire]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/aspire]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/aspire]
```

**重點說明**：

- **Pipeline 架構**：`receivers → processors → exporters`，資料流向清楚
- **batch processor**：批量處理，減少網路呼叫次數
- **CORS 設定**：前端瀏覽器需要透過 HTTP 發送遙測資料，因此 HTTP receiver 需要設定 CORS
- Traces 同時匯出到 Jaeger 和 Aspire Dashboard；Metrics 和 Logs 只送 Aspire Dashboard

### 資料流向總覽

| 來源 | 訊號 | 傳輸方式 | 目的地 |
|------|------|---------|--------|
| Frontend (瀏覽器) | Traces | OTLP HTTP → Collector | Jaeger + Aspire |
| Frontend Server (Node.js) | Traces | OTLP HTTP → Collector | Jaeger + Aspire |
| Backend-A / B | Traces | OTLP gRPC → Collector | Jaeger + Aspire |
| Backend-A / B | Logs (Serilog) | OTLP gRPC → Collector | Aspire |
| Backend-A / B | Logs (Serilog) | Seq Sink (直連) | Seq |
| Backend-A / B | Metrics | OTLP gRPC → Collector | Aspire |

---

## OpenTelemetry 整合 HttpClient、Serilog、Aspire

### 整合 HttpClient - 自動追蹤服務間呼叫

安裝 `OpenTelemetry.Instrumentation.Http` 後，所有透過 `HttpClient` 的呼叫都會自動被追蹤。

Backend-A 的 WeatherController 呼叫 Backend-B 的範例：

```csharp
[HttpGet]
public async Task<IActionResult> Get()
{
    using (LogContext.PushProperty("Action", "GetWeatherForecastFromBackendA"))
    using (LogContext.PushProperty("UserID", "user-123"))
    using (LogContext.PushProperty("ProductID", "prod-456"))
    {
        _logger.LogInformation("Calling Backend-B weatherforecast endpoint at {BackendBUrl}", _backendBUrl);
        var response = await _httpClient.GetStringAsync($"{_backendBUrl}/Weather");
        _logger.LogInformation("Successfully received response from Backend-B.");
        return Content(response, "application/json");
    }
}
```

這段程式碼不需要手動處理追蹤，OpenTelemetry 的 Instrumentation 會自動：

1. 為 `_httpClient.GetStringAsync` 建立一個 child span
2. 在 HTTP 請求中注入 `traceparent` header
3. Backend-B 收到請求後，自動讀取 `traceparent` 並建立下一層 child span

### 整合 Serilog - 結構化日誌 + OTLP 匯出

Serilog 需要安裝以下套件：

```xml
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="9.0.0" />
```

#### Serilog 配置（Program.cs）

```csharp
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
var otlpGrpcEndpoint = Environment.GetEnvironmentVariable("OTLP_GRPC_ENDPOINT")
    ?? "http://localhost:4317";

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "backend-a")
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(seqUrl)
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpGrpcEndpoint;
        options.Protocol = OtlpProtocol.Grpc;
    })
    .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();
```

**重點說明**：

- `Enrich.WithProperty("Application", "backend-a")`：為所有日誌加上 Application 屬性
- `Enrich.FromLogContext()`：支援 `LogContext.PushProperty` 動態加入屬性
- `WriteTo.OpenTelemetry`：將日誌透過 OTLP gRPC 送到 OTel Collector，再轉發到 Aspire Dashboard
- `WriteTo.Seq`：日誌同時直連 Seq，不經過 Collector

#### Serilog 與 Trace 的關聯

`Serilog.Sinks.OpenTelemetry` 有一個很重要的特性：它會自動從目前的 `Activity`（即 Span）擷取 `TraceId` 和 `SpanId`，寫入 LogRecord。

這代表同一個請求的日誌和追蹤可以透過 TraceId 關聯在一起。在 Seq 或 Aspire Dashboard 中，可以用 TraceId 篩選出同一條追蹤鏈的所有日誌。

#### 結構化日誌屬性

使用 `LogContext.PushProperty` 可以為特定範圍內的日誌加入結構化屬性：

```csharp
using (LogContext.PushProperty("Action", "GetWeatherForecastFromBackendA"))
using (LogContext.PushProperty("UserID", "user-123"))
using (LogContext.PushProperty("ProductID", "prod-456"))
{
    _logger.LogInformation("Calling Backend-B...");
}
```

這些屬性會出現在 Seq 和 Aspire Dashboard 的日誌詳細資訊中，便於篩選和查詢。

#### 啟用 Serilog Request Logging

```csharp
builder.Host.UseSerilog();

// ...

app.UseSerilogRequestLogging();
```

`UseSerilogRequestLogging` 會為每個 HTTP 請求產生一筆結構化日誌，包含 HTTP Method、Path、Status Code、耗時等資訊。

### 整合 Aspire Dashboard - 開發階段的遙測儀表板

.NET Aspire Dashboard 可以獨立使用，不需要完整的 Aspire 框架。它透過 OTLP 接收遙測資料，提供 Traces、Metrics、Logs 的整合檢視。

#### Docker Compose 配置

```yaml
aspire-dashboard:
  image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
  container_name: aspire-dashboard
  ports:
    - "18888:18888"
  environment:
    - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
```

**注意事項**：

- Dashboard UI 埠是 `18888`，OTLP 接收埠是 `18889`（容器內部）
- `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`：停用驗證，開發環境適用
- 資料存在記憶體中，Dashboard 重啟後遙測資料會遺失

#### 透過 OTel Collector 轉發

本專案的做法是讓應用程式的遙測資料先發到 OTel Collector，再由 Collector 轉發到 Aspire Dashboard：

```yaml
exporters:
  otlp/aspire:
    endpoint: aspire-dashboard:18889
    tls:
      insecure: true
```

這種架構的好處是應用程式只需要知道 Collector 的位址，後端可以隨時增減，不影響應用程式端。

---

## Docker Compose 完整配置

```yaml
version: '3.8'

services:
  frontend:
    build:
      context: ./src/frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NUXT_BACKEND_A_URL=http://backend-a:8080
      - NUXT_OTEL_EXPORTER_URL=http://otel-collector:4318
    depends_on:
      - backend-a
      - jaeger
      - aspire-dashboard
      - otel-collector
      - seq

  backend-a:
    build:
      context: ./src/backend-a
      dockerfile: Dockerfile
    ports:
      - "5100:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - SEQ_URL=http://seq:80
      - OTLP_GRPC_ENDPOINT=http://otel-collector:4317
      - OTLP_HTTP_ENDPOINT=http://otel-collector:4318
      - BACKEND_B_URL=http://backend-b:8080
    depends_on:
      - jaeger
      - aspire-dashboard
      - otel-collector
      - seq
      - backend-b

  backend-b:
    build:
      context: ./src/backend-b
      dockerfile: Dockerfile
    ports:
      - "5200:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - SEQ_URL=http://seq:80
      - OTLP_GRPC_ENDPOINT=http://otel-collector:4317
      - OTLP_HTTP_ENDPOINT=http://otel-collector:4318
    depends_on:
      - jaeger
      - aspire-dashboard
      - otel-collector
      - seq

  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    command: ["--config=/etc/otelcol/config.yaml"]
    volumes:
      - ./data/otel-collector/otel-collector-config.yaml:/etc/otelcol/config.yaml:ro
    ports:
      - "4317:4317"
      - "4318:4318"
    depends_on:
      - jaeger
      - aspire-dashboard

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"
    environment:
      - COLLECTOR_OTLP_ENABLED=true

  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      - ACCEPT_EULA=Y

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports:
      - "18888:18888"
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true

networks:
  default:
    name: opentelemetry-lab
    driver: bridge
```

---

## 前端追蹤配置

前端使用 OpenTelemetry Web SDK，在瀏覽器中自動追蹤 fetch 請求。

### Nuxt Plugin（opentelemetry.client.ts）

```typescript
import { context, propagation } from '@opentelemetry/api'
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web'
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { resourceFromAttributes } from '@opentelemetry/resources'
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { W3CTraceContextPropagator } from '@opentelemetry/core'

export default defineNuxtPlugin(() => {
  if (import.meta.server) return

  const config = useRuntimeConfig()
  const collectorUrl = config.public.otelCollectorUrl as string

  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: 'frontend',
  })

  const exporter = new OTLPTraceExporter({
    url: `${collectorUrl}/v1/traces`,
  })

  const provider = new WebTracerProvider({
    resource,
    spanProcessors: [new BatchSpanProcessor(exporter)],
  })

  provider.register()

  const contextManager = new ZoneContextManager()
  contextManager.enable()
  context.setGlobalContextManager(contextManager)

  propagation.setGlobalPropagator(new W3CTraceContextPropagator())

  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: [/\/api\//],
      }),
    ],
  })
})
```

**重點說明**：

- `ZoneContextManager`：Zone.js 提供的 Context Manager，確保瀏覽器非同步操作中的追蹤上下文不會遺失
- `W3CTraceContextPropagator`：自動在 fetch 請求中注入 `traceparent` header
- `FetchInstrumentation`：自動 instrument 所有 fetch 請求
- `propagateTraceHeaderCorsUrls`：只對匹配 `/api/` 的 URL 注入追蹤 header

### Nuxt Server 追蹤配置

Nuxt Server 端除了瀏覽器端的 OTel Web SDK，也需要初始化 Node.js 端的 OTel SDK，才能產生 server-side span。

#### Server Plugin（初始化 NodeTracerProvider）

```typescript
// server/plugins/otel.ts
import { NodeTracerProvider } from '@opentelemetry/sdk-trace-node'
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { resourceFromAttributes } from '@opentelemetry/resources'
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { W3CTraceContextPropagator } from '@opentelemetry/core'
import { propagation } from '@opentelemetry/api'

export default defineNitroPlugin(() => {
  const config = useRuntimeConfig()
  const exporterUrl = `${config.otelExporterUrl}/v1/traces`

  const provider = new NodeTracerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'frontend-server',
    }),
    spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({ url: exporterUrl }),
      ),
    ],
  })

  provider.register()
  propagation.setGlobalPropagator(new W3CTraceContextPropagator())
})
```

#### Tracing Middleware（建立 server span）

```typescript
// server/middleware/tracing.ts
// 自動從 request headers 提取 trace context，建立 server span
// 將 OTel context 存入 event.context.otelContext 供 API handler 使用
```

#### fetchWithTracing（建立 client span）

```typescript
// server/utils/tracing.ts
// fetchWithTracing(event, url, options)
// 包裹 $fetch 並自動建立 SpanKind.CLIENT span + 注入 traceparent header
```

#### API Route 使用 fetchWithTracing

```typescript
// server/api/weather.get.ts
export default defineEventHandler(async (event) => {
  const { backendAUrl } = useRuntimeConfig()
  return await fetchWithTracing(event, '/Weather', {
    baseURL: backendAUrl,
    headers: { ...getProxyRequestHeaders(event) },
  })
})
```

**重點說明**：

- `NodeTracerProvider`：Node.js 端的 TracerProvider，service name 為 `frontend-server`（區別於瀏覽器端的 `frontend`）
- Tracing middleware 建立 **server span**，提取 incoming trace context
- `fetchWithTracing` 建立 **client span**，注入 outgoing trace context
- Nuxt config 設定 `nitro.noExternals: true`，將所有依賴 inline 到 bundle，避免 CJS/ESM 不相容問題

---

## 驗證方式

啟動所有服務後，可以透過以下方式驗證：

| 驗證項目 | 操作方式 |
|---------|---------|
| 分散式追蹤 | 開啟 Jaeger UI（http://localhost:16686），查看 frontend → frontend-server → backend-a → backend-b 的完整 Trace（6 個 span） |
| 結構化日誌 | 開啟 Seq（http://localhost:5341），查看包含 TraceId 的日誌 |
| 整合儀表板 | 開啟 Aspire Dashboard（http://localhost:18888），查看 Traces / Metrics / Logs |
| 天氣 API | 開啟 Frontend（http://localhost:3000），操作天氣查詢功能 |

---

## 疑難排解：Clock Skew（時鐘偏差）

### 問題現象

在 Jaeger UI 查看 Trace 時，可能會發現子 span（backend-a）的開始時間比父 span（frontend）更早，時序出現因果關係倒置。Jaeger 會顯示類似以下警告：

```
clock skew adjustment disabled; not applying calculated delta of 4.32317975s
```

### 根因分析

本專案的追蹤鏈跨越了不同的時鐘來源：

| Span 來源 | 時鐘來源 |
|-----------|---------|
| frontend (root span) | 瀏覽器端（使用者的 Windows 主機時鐘） |
| backend-a / backend-b | Docker 容器（WSL2 核心時鐘） |

當 Windows 主機時鐘與 WSL2/Docker 時鐘不同步時（常見於休眠/喚醒後），就會出現 Clock Skew。

以實際的 Trace 數據為例：

```
frontend (HTTP GET, root):   startTime = 1771085788976000 μs
backend-a (GET Weather):     startTime = 1771085784654696 μs
                             差異 = 4.32 秒（子 span 比父 span 早）
```

### 解決方式

**方案 1：同步 WSL 時鐘（推薦）**

```bash
# 安裝 ntpdate
sudo apt-get install -y ntpdate

# 使用 NTP 校時
sudo ntpdate time.google.com
```

同時在 Windows 端同步時間：設定 → 時間與語言 → 日期和時間 → 立即同步。


---

## 範例原始碼

完整範例放在 GitHub：https://github.com/nickyc975/Lab.OpenTelemetrySerilog

若有謬誤，煩請告知，新手發帖請多包涵。
