# ASP.NET Core 整合 OpenTelemetry 與 Serilog 實踐可觀測性

## 前言

微服務架構下，請求會在多個服務間流轉，單靠傳統日誌很難追蹤完整呼叫鏈路。OpenTelemetry 提供標準化的遙測資料收集方式，整合 Serilog 可以將結構化日誌與分散式追蹤關聯起來，讓問題排查更有效率。

這篇文章會示範如何在 ASP.NET Core 中整合 OpenTelemetry、Serilog，並使用 Jaeger、Seq、Aspire Dashboard 進行可觀測性監控。

## 開發環境

- Windows 11 / WSL2
- ASP.NET Core 10.0
- OpenTelemetry SDK 1.15.0
- Serilog 10.0.0
- Docker Compose 3.8

---

## OpenTelemetry 三大訊號

OpenTelemetry 定義了三種遙測資料：

| 訊號類型 | 用途 | 範例 |
|---------|------|------|
| **Traces** | 分散式追蹤，追蹤請求在服務間的流轉 | 請求從前端 → API-A → API-B 的完整鏈路 |
| **Metrics** | 效能指標，如請求量、回應時間 | HTTP 請求數、錯誤率 |
| **Logs** | 結構化日誌，記錄事件詳細資訊 | 使用者操作、錯誤訊息 |

---

## 架構設計

### 系統架構圖

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Frontend   │────▶│  Backend-A   │────▶│  Backend-B   │
│   (Nuxt 4)   │     │ ASP.NET 10   │     │ ASP.NET 10   │
└──────────────┘     └──────┬───────┘     └──────┬───────┘
                            │                    │
                     Serilog│+ OTel SDK   Serilog│+ OTel SDK
                            │                    │
                            ▼                    ▼
                    ┌───────────────────────────────┐
                    │   OTel Collector (OTLP)       │
                    └───┬──────────┬────────────┬───┘
                        │          │            │
                        ▼          ▼            ▼
                    ┌───────┐ ┌─────────┐ ┌─────────────┐
                    │Jaeger │ │   Seq   │ │   Aspire    │
                    │Traces │ │  Logs   │ │  Dashboard  │
                    └───────┘ └─────────┘ └─────────────┘
```

### 資料流向

| 來源 | 訊號類型 | 傳輸協定 | 目的地 |
|------|---------|---------|--------|
| Frontend | Traces | OTLP HTTP (4318) | OTel Collector → Jaeger + Aspire |
| Backend-A/B | Traces | OTLP gRPC (4317) | OTel Collector → Jaeger + Aspire |
| Backend-A/B | Logs | OTLP gRPC + Seq Sink | OTel Collector + Seq |
| Backend-A/B | Metrics | OTLP gRPC (4317) | OTel Collector → Aspire |

---

## ASP.NET Core 整合 OpenTelemetry

### 安裝 NuGet 套件

```xml
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="9.0.0" />
```

### 設定 OpenTelemetry

在 `Program.cs` 中設定 OpenTelemetry：

```csharp
var otlpGrpcEndpoint = Environment.GetEnvironmentVariable("OTLP_GRPC_ENDPOINT") ?? "http://localhost:4317";
var otlpHttpEndpoint = Environment.GetEnvironmentVariable("OTLP_HTTP_ENDPOINT") ?? "http://localhost:4318";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("backend-a"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()    // 自動追蹤 ASP.NET Core 請求
        .AddHttpClientInstrumentation()    // 自動追蹤 HttpClient 呼叫
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

### 關鍵設定說明

| 設定項目 | 說明 |
|---------|------|
| `AddService("backend-a")` | 設定服務名稱，用於追蹤時識別服務 |
| `AddAspNetCoreInstrumentation()` | 自動追蹤 HTTP 請求，包含路由、狀態碼 |
| `AddHttpClientInstrumentation()` | 自動追蹤 HttpClient 呼叫，建立子 Span |
| `AddOtlpExporter()` | 設定 OTLP 匯出器，支援 gRPC/HTTP 協定 |

---

## Serilog 整合 OpenTelemetry

### 設定 Serilog

```csharp
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
var otlpGrpcEndpoint = Environment.GetEnvironmentVariable("OTLP_GRPC_ENDPOINT") ?? "http://localhost:4317";

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

builder.Host.UseSerilog();
app.UseSerilogRequestLogging();
```

### Enricher 說明

| Enricher | 用途 |
|----------|------|
| `WithProperty("Application", "backend-a")` | 固定屬性，所有日誌自動加上應用程式名稱 |
| `FromLogContext()` | 啟用 LogContext，支援動態新增屬性 |

### 結構化日誌與 TraceId 關聯

使用 `LogContext` 新增上下文資訊：

```csharp
using (LogContext.PushProperty("Action", "GetWeatherForecast"))
using (LogContext.PushProperty("UserID", "user-123"))
using (LogContext.PushProperty("ProductID", "prod-456"))
{
    _logger.LogInformation("Calling Backend-B...");
    var response = await _httpClient.GetStringAsync($"{_backendBUrl}/Weather");
    _logger.LogInformation("Successfully received response from Backend-B.");
    return Content(response, "application/json");
}
```

**優點**：
- 日誌自動包含 TraceId、SpanId
- 可在 Seq 中用 TraceId 查詢，直接關聯到 Jaeger Trace
- LogContext 在 using 結束後自動移除屬性，避免汙染

---

## OTel Collector 設定

### 設定檔案

`data/otel-collector/otel-collector-config.yaml`：

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317      # Backend 使用
      http:
        endpoint: 0.0.0.0:4318      # Frontend 使用
        cors:
          allowed_origins:
            - "http://localhost:3000"

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

### 設定說明

| 元件 | 用途 |
|------|------|
| `receivers.otlp` | 接收 OTLP 資料 (gRPC/HTTP) |
| `processors.batch` | 批次處理，提高傳輸效率 |
| `exporters.otlp/jaeger` | 匯出 Traces 到 Jaeger |
| `exporters.otlp/aspire` | 匯出所有訊號到 Aspire Dashboard |

---

## 分散式追蹤傳播

### W3C Trace Context

OpenTelemetry 使用 W3C Trace Context 標準，透過 HTTP Header 傳遞追蹤資訊：

```
traceparent: 00-{trace-id}-{span-id}-01
```

### 追蹤鏈路

```
Browser (產生 traceparent)
  └──▶ Nuxt Server Proxy (透傳 traceparent)
         └──▶ Backend-A (讀取 traceparent, 建立 child span)
                └──▶ Backend-B (讀取 traceparent, 建立 child span)
```

**自動化機制**：
- `AddAspNetCoreInstrumentation()`：自動讀取請求的 `traceparent` header
- `AddHttpClientInstrumentation()`：自動在 HttpClient 請求中注入 `traceparent` header
- 無需手動處理，SDK 自動建立正確的 parent-child 關係

---

## Aspire Dashboard 整合

### 特色

- 整合 Traces、Metrics、Logs 三種訊號
- 提供統一儀表板，不需切換工具
- 支援 OTLP 協定，與 OpenTelemetry 無縫整合

### Docker Compose 設定

```yaml
aspire-dashboard:
  image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
  container_name: aspire-dashboard
  ports:
    - "18888:18888"
  environment:
    - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
  networks:
    - opentelemetry-lab
```

**存取位置**：http://localhost:18888

---

## 實際應用場景

### 場景 1：追蹤 API 呼叫鏈路

**問題**：前端請求失敗，不確定是哪個服務出問題

**解決方式**：
1. 在 Jaeger UI (http://localhost:16686) 搜尋 Service: `frontend`
2. 找到失敗的 Trace，展開 Span 查看完整呼叫鏈路
3. 定位到 `backend-b` 的 Span 回傳 500 錯誤
4. 複製 TraceId，到 Seq 查詢該 TraceId 的日誌
5. 找到錯誤訊息：`NullReferenceException`

### 場景 2：監控效能瓶頸

**問題**：使用者回報系統變慢

**解決方式**：
1. 在 Aspire Dashboard 查看 Metrics
2. 發現 `backend-a` 的 HTTP 請求延遲從 100ms 升到 2000ms
3. 在 Jaeger 查看 Trace，發現 `backend-b` 的 Span 耗時 1.8s
4. 在 Seq 查詢 `backend-b` 的日誌，發現資料庫查詢慢
5. 優化 SQL 查詢，問題解決

---

## 驗證方式

### 1. 啟動服務

```bash
docker-compose up -d
```

### 2. 測試 API

```bash
# 查詢天氣
curl http://localhost:5100/Weather

# 新增天氣
curl -X POST http://localhost:5100/Weather \
  -H "Content-Type: application/json" \
  -d '{"date":"2026-02-10","temperatureC":25,"summary":"Sunny"}'
```

### 3. 檢視追蹤

- **Jaeger**：http://localhost:16686
  - 搜尋 Service: `backend-a`
  - 查看完整 Trace 鏈路
  
- **Seq**：http://localhost:5341
  - 使用 `TraceId = "{trace-id}"` 查詢日誌
  - 檢視結構化日誌屬性

- **Aspire Dashboard**：http://localhost:18888
  - Traces 頁面：查看追蹤
  - Metrics 頁面：查看效能指標
  - Logs 頁面：查看日誌

---

## 最佳實踐

### 1. 使用 LogContext 新增上下文

```csharp
using (LogContext.PushProperty("OrderId", orderId))
using (LogContext.PushProperty("UserId", userId))
{
    _logger.LogInformation("Processing order");
}
```

### 2. 善用物件解構

```csharp
var order = new { OrderId = 123, Amount = 100 };
_logger.LogInformation("Order received: {@Order}", order);
```

### 3. 設定環境變數統一管理端點

```yaml
environment:
  - OTLP_GRPC_ENDPOINT=http://otel-collector:4317
  - OTLP_HTTP_ENDPOINT=http://otel-collector:4318
  - SEQ_URL=http://seq:80
```

### 4. 批次處理提高效能

在 OTel Collector 中使用 `batch` processor：

```yaml
processors:
  batch:
    timeout: 10s
    send_batch_size: 1024
```

---

## 常見問題

### Q1：為什麼 Jaeger 看不到 Trace？

**檢查項目**：
- OTel Collector 是否正常運行
- Backend 的 OTLP_GRPC_ENDPOINT 設定是否正確
- 檢查 OTel Collector logs：`docker logs otel-collector`

### Q2：Seq 中的日誌沒有 TraceId？

**解決方式**：
- 確認 Serilog 有設定 `.WriteTo.OpenTelemetry()`
- 確認有呼叫 `app.UseSerilogRequestLogging()`

### Q3：Aspire Dashboard 連線失敗？

**解決方式**：
- 確認 Docker network 設定正確
- 確認 OTel Collector 的 `exporters.otlp/aspire.endpoint` 設定為 `aspire-dashboard:18889`

### Q4：Jaeger 出現 Clock Skew 警告，Span 時序倒置？

在 Jaeger UI 查看 Trace 時，可能會發現子 span（backend-a）的開始時間比父 span（frontend）更早，Jaeger 顯示類似以下警告：

```
clock skew adjustment disabled; not applying calculated delta of 4.32317975s
```

**根因**：本專案的追蹤鏈跨越不同時鐘來源：

| Span 來源 | 時鐘來源 |
|-----------|---------|
| frontend (root span) | 瀏覽器端（使用者的 Windows 主機時鐘） |
| backend-a / backend-b | Docker 容器（WSL2 核心時鐘） |

當 Windows 主機時鐘與 WSL2/Docker 時鐘不同步時（常見於休眠/喚醒後），就會出現 Clock Skew。

**解決方式**：同步 WSL 時鐘

```bash
# 安裝 ntpdate
sudo apt-get install -y ntpdate

# 使用 NTP 校時
sudo ntpdate time.google.com
```

同時在 Windows 端同步時間：設定 → 時間與語言 → 日期和時間 → 立即同步。

---

## 心得

OpenTelemetry 提供標準化的遙測資料收集方式，整合 Serilog 可以將結構化日誌與分散式追蹤關聯起來。透過 OTel Collector 統一收集資料，再分發到 Jaeger、Seq、Aspire Dashboard，讓可觀測性監控更完整。

這次實作的重點：
- **自動化**：使用 Instrumentation 自動追蹤 HTTP 請求與 HttpClient 呼叫
- **關聯性**：LogContext 讓日誌自動包含 TraceId，方便關聯
- **標準化**：使用 OTLP 協定，支援多種後端工具

微服務架構下，可觀測性是必備能力，OpenTelemetry + Serilog 是值得投資的組合。

---

## 參考資料

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Serilog Documentation](https://serilog.net/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)

---

若有謬誤，煩請告知，新手發帖請多包涵。
