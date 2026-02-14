# OpenTelemetry Lab 專案架構分析

## 1. 服務架構概覽

架構概覽

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

### 核心服務列表

| 服務名稱 | 技術棧 | 本機端口 | 容器端口 | 功能說明 |
|---------|-------|--------|---------|---------|
| **frontend** | Nuxt 4 + Vue 3 + Node.js 22 | 3000 | 3000 | 前端 UI，天氣查詢/新增介面 |
| **backend-a** | ASP.NET Core 10 | 5100 | 8080 | API 閘道，呼叫 backend-b，支援 GET/POST /Weather |
| **backend-b** | ASP.NET Core 10 | 5200 | 8080 | 核心服務，生成/存儲天氣資料，支援 GET/POST /Weather |

### 基礎設施服務

| 服務名稱 | 技術 | 本機端口 | 用途 |
|---------|-----|--------|------|
| **otel-collector** | OpenTelemetry Collector Contrib | 4317 (gRPC), 4318 (HTTP) | 接收 & 轉發 Traces/Metrics/Logs |
| **jaeger** | Jaeger All-in-One | 16686 | 分散式追蹤 UI |
| **seq** | Seq | 5341 | 結構化日誌 UI |
| **aspire-dashboard** | .NET Aspire Dashboard | 18888 | 整合 Traces/Metrics/Logs 儀表板 |

---

## 2. 服務間呼叫鏈

### 流水線追蹤鏈路
```
Browser (OTel fetch span, 注入 traceparent)
  └──▶ Nuxt Server Proxy (/api/weather → backend-a:8080/Weather, 透傳 traceparent)
         └──▶ API-A (讀取 traceparent, 建立 child span, 呼叫 API-B)
                └──▶ API-B (讀取 traceparent, 建立 child span)
```
---

### 資料流向

| 訊號類型 | 來源 | 傳輸方式 | 目的地 |
|---------|------|---------|--------|
| Traces | Frontend (OTel Web SDK) | OTLP HTTP → OTel Collector | Jaeger + Aspire Dashboard |
| Traces | API-A / API-B (OTel SDK) | OTLP gRPC → OTel Collector | Jaeger + Aspire Dashboard |
| Logs | API-A / API-B (Serilog) | OTLP → OTel Collector | Aspire Dashboard |
| Logs | API-A / API-B (Serilog) | Serilog Seq Sink (直連) | Seq |
| Logs | API-A / API-B (Serilog) | Serilog Console Sink | Console (stdout) |
| Metrics | API-A / API-B (OTel SDK) | OTLP gRPC → OTel Collector | Aspire Dashboard |

### 技術規格

| 項目 | 技術 | 版本 |
|------|------|------|
| Frontend | Nuxt 4 + Vue 3 | latest |
| Backend | ASP.NET Core | 10.0 |
| Logging | Serilog | latest |
| Tracing | OpenTelemetry .NET SDK | latest |
| Collector | OpenTelemetry Collector Contrib | latest |
| Trace UI | Jaeger | latest |
| Log UI | Seq | latest |
| Dashboard | Aspire Dashboard | latest |
| 容器編排 | Docker Compose | v3.8 |

## 3. OpenTelemetry 追蹤傳播

### 3.1 前端 (Frontend) 追蹤配置

**檔案**: `/src/frontend/app/plugins/opentelemetry.client.ts`

```typescript
// 核心組件
- WebTracerProvider (service: "frontend")
- BatchSpanProcessor (OTLP HTTP exporter)
- OTLPTraceExporter (端點: http://localhost:4318/v1/traces)
- W3CTraceContextPropagator (自動注入 traceparent header)
- FetchInstrumentation (自動 instrument 所有 fetch 請求)
- ZoneContextManager (瀏覽器非同步 context 傳播)
```

**傳播機制**:
- 瀏覽器 fetch 請求自動被 instrumented
- 自動注入 `traceparent` header (W3C Trace Context 標準)
- CORS 允許列表: `http://localhost:3000` (OTel Collector 配置)

### 3.2 Nuxt Server Proxy

**檔案**: `/src/frontend/nuxt.config.ts`

```typescript
runtimeConfig: {
  backendAUrl: 'http://localhost:5100',
  public: {
    otelCollectorUrl: process.env.OTEL_COLLECTOR_URL || 'http://localhost:4318',
  },
}
```

**API 路由**:
- `GET /api/weather` → `/src/frontend/server/api/weather.get.ts`
- `POST /api/weather` → `/src/frontend/server/api/weather.post.ts`

**透傳行為**:
```typescript
// weather.get.ts
const response = await $fetch('/Weather', {
  baseURL: backendAUrl,
  headers: getProxyRequestHeaders(event),  // ← 透傳 traceparent header
})

// weather.post.ts
headers: Object.fromEntries(
  Object.entries(getProxyRequestHeaders(event))
    .filter(([key]) => key.toLowerCase() !== 'content-length'),  // 移除 content-length 避免衝突
)
```

### 3.3 Backend-A 追蹤配置

**檔案**: `/src/backend-a/Program.cs`

```csharp
// 服務名稱
.ConfigureResource(resource => resource.AddService("backend-a"))

// Tracing 設定
.WithTracing(tracing => tracing
    .AddAspNetCoreInstrumentation()    // 自動 instrument ASP.NET Core 請求
    .AddHttpClientInstrumentation()    // 自動 instrument HttpClient 呼叫
    .AddOtlpExporter("otlp-grpc", options => {
        options.Endpoint = new Uri(otlpGrpcEndpoint);  // http://otel-collector:4317
        options.Protocol = OtlpExportProtocol.Grpc;
    })
    .AddOtlpExporter("otlp-http", options => {
        options.Endpoint = new Uri(otlpHttpEndpoint);  // http://otel-collector:4318
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
    }))

// Metrics 設定 (使用相同 exporter)
.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(...)
)
```

**WeatherController 實作**:

```csharp
// GET 端點 - 呼叫 Backend-B
[HttpGet]
public async Task<IActionResult> Get()
{
    using (LogContext.PushProperty("Action", "GetWeatherForecastFromBackendA"))
    using (LogContext.PushProperty("UserID", "user-123"))
    using (LogContext.PushProperty("ProductID", "prod-456"))
    {
        var response = await _httpClient.GetStringAsync($"{_backendBUrl}/Weather");
        return Content(response, "application/json");
    }
}

// POST 端點 - 轉發到 Backend-B
[HttpPost]
public async Task<IActionResult> Post(WeatherForecast forecast)
{
    var response = await _httpClient.PostAsJsonAsync($"{_backendBUrl}/weather", forecast);
    return Ok(forecast);
}
```

**Trace 上下文傳播**:
- ASP.NET Core instrumentation 自動讀取 `traceparent` header
- 建立 child span 並設定正確的 parent-child 關係
- 透過 HttpClient instrumentation 自動為 Backend-B 的呼叫創建子 span

### 3.4 Backend-B 追蹤配置

**檔案**: `/src/backend-b/Program.cs`

配置同 Backend-A，服務名稱為 `"backend-b"`

```csharp
.ConfigureResource(resource => resource.AddService("backend-b"))
```

**WeatherController 實作**:

```csharp
// GET 端點 - 生成隨機天氣資料 (終端點，無進一步呼叫)
[HttpGet]
public IEnumerable<WeatherForecast> Get()
{
    using (LogContext.PushProperty("Action", "GenerateWeatherForecastForBackendB"))
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            )
        ).ToArray();
        return forecast;
    }
}

// POST 端點 - 模擬存儲
[HttpPost]
public IActionResult Post(WeatherForecast forecast)
{
    using (LogContext.PushProperty("Action", "AddWeatherForecastToBackendB"))
    {
        _logger.LogInformation("Weather forecast added successfully (simulated).");
        return Ok(forecast);
    }
}
```

---

## 4. 完整追蹤流程示例

```
Jaeger Trace (單一 Trace ID)
├─ Span: browser.GET /api/weather
│   └─ traceparent: 01-{trace-id}-{span-id}-01
│
├─ Span: frontend (OTel Plugin 發送)
│   └─ OTLP HTTP → OTel Collector
│
├─ Span: backend-a.GET /Weather (收到 traceparent)
│   │   parent_span_id: {上一層 span-id}
│   │
│   └─ Span: backend-a.HttpClient.GET backend-b (child)
│       └─ 注入 traceparent header 到 Backend-B 請求
│
└─ Span: backend-b.GET /Weather (收到 traceparent)
    parent_span_id: {Backend-A 的 HttpClient span}
```

---

## 5. Serilog 日誌配置

**位置**: Backend-A/Backend-B 的 `Program.cs`

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "backend-a")
    .Enrich.FromLogContext()  // 自動包含 LogContext 中的自訂屬性
    .WriteTo.Console()        // 標準輸出
    .WriteTo.Seq(seqUrl)      // 直連 Seq (http://seq:80)
    .WriteTo.OpenTelemetry(options => {
        options.Endpoint = otlpGrpcEndpoint;  // http://otel-collector:4317
        options.Protocol = OtlpProtocol.Grpc;
    })
    .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger()
```

**日誌結構化屬性** (透過 LogContext):
```csharp
using (LogContext.PushProperty("Action", "GetWeatherForecastFromBackendA"))
using (LogContext.PushProperty("UserID", "user-123"))
using (LogContext.PushProperty("ProductID", "prod-456"))
{
    _logger.LogInformation("Calling Backend-B...");
}
// 輸出: Action=GetWeatherForecastFromBackendA, UserID=user-123, ProductID=prod-456
```

---

## 6. OpenTelemetry Collector 配置

**檔案**: `/data/otel-collector/otel-collector-config.yaml`

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317      # Backend 端 (gRPC)
      http:
        endpoint: 0.0.0.0:4318      # Frontend 端 (HTTP)
        cors:
          allowed_origins:
            - "http://localhost:3000"  # 瀏覽器 CORS
          allowed_headers:
            - "*"

exporters:
  otlp/jaeger:
    endpoint: jaeger:4317           # Jaeger traces
    tls:
      insecure: true

  otlp/aspire:
    endpoint: aspire-dashboard:18889  # Aspire Dashboard (traces/metrics/logs)
    tls:
      insecure: true

processors:
  batch:                            # 批量處理 (提高效率)

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

### 數據流向

| 來源 | 訊號類型 | 傳輸方式 | 目的地 |
|------|---------|---------|--------|
| Frontend (瀏覽器) | Traces | OTLP HTTP (port 4318) | OTel Collector → Jaeger + Aspire |
| Backend-A/B | Traces | OTLP gRPC (port 4317) | OTel Collector → Jaeger + Aspire |
| Backend-A/B | Logs (Serilog) | OTLP gRPC + Seq Sink | OTel Collector + Seq (直連) |
| Backend-A/B | Metrics | OTLP gRPC (port 4317) | OTel Collector → Aspire |

---

## 7. Docker Compose 服務依賴

```
frontend
├─ depends_on: [backend-a, jaeger, aspire-dashboard, otel-collector, seq]
│
backend-a
├─ depends_on: [backend-b, jaeger, aspire-dashboard, otel-collector, seq]
│  environment:
│    - ASPNETCORE_URLS=http://+:8080
│    - BACKEND_B_URL=http://backend-b:8080
│    - SEQ_URL=http://seq:80
│    - OTLP_GRPC_ENDPOINT=http://otel-collector:4317
│    - OTLP_HTTP_ENDPOINT=http://otel-collector:4318
│
backend-b
├─ depends_on: [jaeger, aspire-dashboard, otel-collector, seq]
│  environment: (同 backend-a，但無 BACKEND_B_URL)
│
otel-collector
├─ depends_on: [jaeger, aspire-dashboard]
│
jaeger, seq, aspire-dashboard
└─ 無依賴

network: opentelemetry-lab (bridge mode)
```

---

## 8. NuGet 與 npm 依賴

### Backend (.NET)

```xml
<!-- backend-a.csproj / backend-b.csproj -->
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="9.0.0" />
```

### Frontend (Node.js)

```json
{
  "@opentelemetry/api": "^1.9.0",
  "@opentelemetry/context-zone": "^2.5.0",
  "@opentelemetry/exporter-trace-otlp-http": "^0.211.0",
  "@opentelemetry/instrumentation": "^0.211.0",
  "@opentelemetry/instrumentation-fetch": "^0.211.0",
  "@opentelemetry/resources": "^2.5.0",
  "@opentelemetry/sdk-trace-base": "^2.5.0",
  "@opentelemetry/sdk-trace-web": "^2.5.0",
  "@opentelemetry/semantic-conventions": "^1.39.0",
  "nuxt": "^4.3.1",
  "vue": "^3.5.27",
  "vue-router": "^4.6.4"
}
```

---

## 9. 關鍵配置檔案位置

| 檔案 | 功能 |
|------|------|
| `/docker-compose.yml` | 服務編排 |
| `/data/otel-collector/otel-collector-config.yaml` | OTel Collector 配置 |
| `/data/jaeger-ui/jaeger-ui.json` | Jaeger UI 主題配置 |
| `/src/backend-a/Program.cs` | Backend-A 啟動與 OTel 配置 |
| `/src/backend-b/Program.cs` | Backend-B 啟動與 OTel 配置 |
| `/src/backend-a/appsettings.json` | Backend-A 日誌設定 |
| `/src/backend-b/appsettings.json` | Backend-B 日誌設定 |
| `/src/frontend/nuxt.config.ts` | Frontend 配置 (runtimeConfig、proxy rules) |
| `/src/frontend/app/plugins/opentelemetry.client.ts` | Frontend OTel 初始化 |
| `/src/frontend/app/pages/index.vue` | 天氣查詢/新增 UI |
| `/src/frontend/server/api/weather.get.ts` | Nuxt Server API (GET) |
| `/src/frontend/server/api/weather.post.ts` | Nuxt Server API (POST) |

---

## 10. 疑難排解：Clock Skew（時鐘偏差）

### 問題現象

在 Jaeger UI 中查看 Trace 時，發現 backend-a 的 span 開始時間比 frontend 的 root span **更早**，出現因果關係倒置的情況。Jaeger 會顯示警告：

```
clock skew adjustment disabled; not applying calculated delta of 4.32317975s
```

### 根因分析

本專案的追蹤鏈中，時鐘來源不同：

| Span 來源 | 時鐘來源 |
|-----------|---------|
| frontend (root span) | **瀏覽器端**（使用者的 Windows 主機時鐘） |
| backend-a / backend-b | **Docker 容器時鐘**（WSL2 核心時鐘） |

當 Windows 主機時鐘與 WSL2/Docker 時鐘不同步時，就會出現 Clock Skew，導致子 span 的開始時間比父 span 更早。

### 解決方案

#### 方案 1：同步 WSL 時鐘（推薦）

```bash
# 安裝 ntpdate
sudo apt-get install -y ntpdate

# 使用 NTP 校時
sudo ntpdate time.google.com
```

同時需要在 **Windows 端**同步時間：
- 設定 → 時間與語言 → 日期和時間 → 立即同步


若設為 `0s` 則停用自動校正，會顯示真實時序（含偏差）。此方案僅修正顯示，不修正根本原因。

---

## 11. 驗收標準與驗證方式

1. **分散式追蹤**: Jaeger UI (http://localhost:16686) 可查看完整 frontend → backend-a → backend-b 的 Trace 鏈
2. **前端追蹤**: 瀏覽器 fetch 自動帶 `traceparent` header，OTLP HTTP 請求成功發送
3. **日誌關聯**: Seq (http://localhost:5341) 中的日誌包含 TraceId，可與 Jaeger Trace 關聯
4. **整合儀表板**: Aspire Dashboard (http://localhost:18888) 顯示 Traces/Metrics/Logs
5. **天氣介面**: Frontend (http://localhost:3000) 可查詢天氣列表、新增天氣
