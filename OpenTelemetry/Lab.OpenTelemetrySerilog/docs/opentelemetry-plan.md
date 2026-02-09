# OpenTelemetry + Serilog 開發環境建置計畫

## 架構概覽

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

### 流水線追蹤鏈路

```
Browser (OTel fetch span, 注入 traceparent)
  └──▶ Nuxt Server Proxy (/api/weather → backend-a:8080/Weather, 透傳 traceparent)
         └──▶ API-A (讀取 traceparent, 建立 child span, 呼叫 API-B)
                └──▶ API-B (讀取 traceparent, 建立 child span)
```

### 資料流向

| 訊號類型 | 來源 | 傳輸方式 | 目的地 |
|---------|------|---------|--------|
| Traces | Frontend (OTel Web SDK) | OTLP HTTP → OTel Collector | Jaeger + Aspire Dashboard |
| Traces | API-A / API-B (OTel SDK) | OTLP gRPC → OTel Collector | Jaeger + Aspire Dashboard |
| Logs | API-A / API-B (Serilog) | OTLP → OTel Collector | Aspire Dashboard |
| Logs | API-A / API-B (Serilog) | Serilog Seq Sink (直連) | Seq |
| Logs | API-A / API-B (Serilog) | Serilog Console Sink | Console (stdout) |
| Metrics | API-A / API-B (OTel SDK) | OTLP gRPC → OTel Collector | Aspire Dashboard |

## 技術規格

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

## 專案結構

```
Lab.OpenTelemetrySerilog/
├── docker-compose.yml                 # 統一編排所有服務
├── otel-collector-config.yaml         # OTel Collector 設定
├── opentelemetry-plan.md              # 本計畫文件
├── src/
│   ├── frontend/                      # Nuxt 4 前端
│   │   ├── nuxt.config.ts
│   │   ├── package.json
│   │   ├── Dockerfile
│   │   └── ...
│   ├── backend-a/                     # 後端服務 A
│   │   ├── Lab.ApiA.csproj
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   └── ...
│   └── backend-b/                     # 後端 API-B
│       ├── Lab.ApiB.csproj
│       ├── Program.cs
│       ├── Dockerfile
│       └── ...
```

## 服務端口配置

| 服務 | 本機端口 | 容器端口 | 用途 |
|------|---------|---------|------|
| Frontend (Nuxt) | 3000 | 3000 | 前端 UI |
| backend-a | 5100 | 8080 | 後端服務 A |
| backend-b | 5200 | 8080 | 後端服務 B |
| OTel Collector (OTLP gRPC) | 4317 | 4317 | OTLP gRPC 接收 |
| OTel Collector (OTLP HTTP) | 4318 | 4318 | OTLP HTTP 接收 |
| Jaeger UI | 16686 | 16686 | Trace 查詢 UI |
| Seq UI | 5341 | 80 | 結構化日誌查詢 UI |
| Aspire Dashboard | 18888 | 18888 | Traces/Metrics/Logs 整合 UI |

## 實作步驟

### 階段一：專案初始化

- [x] **步驟 1：建立後端專案 backend-a**
  - 使用 `dotnet new webapi` 建立 `src/backend-a`
  - 設定基本的 `appsettings.json`（端口、環境變數）
  - 驗證：`dotnet build` 成功

- [x] **步驟 2：建立後端專案 backend-b**
  - 使用 `dotnet new webapi` 建立 `src/backend-b`
  - 設定基本的 `appsettings.json`（端口、環境變數）
  - 驗證：`dotnet build` 成功

- [x] **步驟 3：建立前端專案 Frontend**
  - 使用 `npx nuxi@latest init` 建立 `src/frontend`
  - 安裝基本依賴
  - 驗證：`npm run dev` 可啟動

### 階段二：Serilog 整合

- [x] **步驟 4：backend-a 整合 Serilog**
  - 安裝 NuGet 套件：
    - `Serilog.AspNetCore`
    - `Serilog.Sinks.Console`
    - `Serilog.Sinks.Seq`
    - `Serilog.Sinks.OpenTelemetry`
  - 在 `Program.cs` 設定 Serilog 三個 Sink（Console / Seq / OTLP）
  - 設定結構化日誌格式
  - 依賴：步驟 1

- [x] **步驟 5：backend-b 整合 Serilog**
  - 同步驟 4，安裝相同套件並設定 Serilog
  - 依賴：步驟 2

### 階段三：OpenTelemetry SDK 整合

- [x] **步驟 6：backend-a 整合 OpenTelemetry**
  - 安裝 NuGet 套件：
    - `OpenTelemetry.Extensions.Hosting`
    - `OpenTelemetry.Instrumentation.AspNetCore`
    - `OpenTelemetry.Instrumentation.Http`
    - `OpenTelemetry.Exporter.OpenTelemetryProtocol`
  - 在 `Program.cs` 設定 Tracing + Metrics，Exporter 指向 OTel Collector
  - 設定 `HttpClient` 注入（用於呼叫 API-B）
  - 依賴：步驟 4

- [x] **步驟 7：backend-b 整合 OpenTelemetry**
  - 同步驟 6，安裝相同套件並設定 OTel
  - 依賴：步驟 5

### 階段三-B：Frontend OpenTelemetry 瀏覽器端追蹤

- [x] **步驟 7a：安裝前端 OTel npm 套件**
  - 安裝 `@opentelemetry/api`、`@opentelemetry/sdk-trace-web`、`@opentelemetry/sdk-trace-base`、`@opentelemetry/exporter-trace-otlp-http`、`@opentelemetry/instrumentation-fetch`、`@opentelemetry/instrumentation`、`@opentelemetry/resources`、`@opentelemetry/semantic-conventions`、`@opentelemetry/context-zone`
  - 依賴：步驟 3

- [x] **步驟 7b：建立 Nuxt OTel client-only plugin**
  - 新增 `src/frontend/plugins/opentelemetry.client.ts`
  - 設定 WebTracerProvider、BatchSpanProcessor、OTLPTraceExporter、ZoneContextManager、W3CTraceContextPropagator、FetchInstrumentation
  - 透過 `runtimeConfig.public.otelCollectorUrl` 讀取可配置的端點
  - 依賴：步驟 7a

- [x] **步驟 7c：設定 Nuxt Server Proxy + runtimeConfig**
  - 更新 `nuxt.config.ts`，新增 `runtimeConfig.public.otelCollectorUrl`
  - 新增 `routeRules` 設定 `/api/weather` → backend-a `/Weather` proxy
  - 依賴：步驟 7b

- [x] **步驟 7d：更新 OTel Collector CORS 設定**
  - 在 `otel-collector-config.yaml` 的 `receivers.otlp.protocols.http` 下新增 CORS
  - 允許來源 `http://localhost:3000`，允許 headers `*`

### 階段四：業務邏輯與追蹤鏈路

- [x] **步驟 8：實作 backend-a 端點**
  - 建立 `GET /Weather` 和 `POST /Weather` 端點
  - `GET` 端點使用 `HttpClient` 呼叫 backend-b 的 `GET /Weather`
  - 加入 Serilog 結構化日誌記錄
  - 依賴：步驟 6

- [x] **步驟 9：實作 backend-b 端點**
  - 建立 `GET /Weather` 端點
  - 回傳模擬的天氣預報資料
  - 加入 Serilog 結構化日誌記錄
  - 依賴：步驟 7

- [x] **步驟 10：實作 Nuxt 前端天氣介面**
  - 修改檔案：`src/frontend/app/app.vue` — 替換 `NuxtWelcome` 為 `NuxtPage`
  - 新增檔案：`src/frontend/app/pages/index.vue`
  - 頁面功能：
    - **查詢天氣**：按鈕觸發 `GET /api/weather`，以表格顯示天氣預報列表
    - **新增天氣**：表單輸入資料，Submit 觸發 `POST /api/weather`，成功後自動重新載入天氣列表
  - 依賴：步驟 7c

### 階段五：基礎建設 (Docker)

- [x] **步驟 11：建立 OTel Collector 設定檔**
  - 建立 `otel-collector-config.yaml`
  - 設定 Receivers：OTLP (gRPC + HTTP)
  - 設定 Exporters：Jaeger (OTLP)、Aspire Dashboard (OTLP)
  - 設定 Pipelines：traces / metrics / logs

- [x] **步驟 12：建立後端 Dockerfile**
  - 為 API-A 和 API-B 各建立 `Dockerfile`
  - 使用 multi-stage build（SDK build → Runtime image）
  - 驗證：`docker build` 成功

- [x] **步驟 13：建立前端 Dockerfile**
  - 為 Nuxt 前端建立 `Dockerfile`
  - 驗證：`docker build` 成功

- [x] **步驟 14：建立 docker-compose.yml**
  - 編排所有服務：
    - `frontend`（Nuxt 4）
    - `backend-a`（ASP.NET Core 10）
    - `backend-b`（ASP.NET Core 10）
    - `otel-collector`（OpenTelemetry Collector Contrib）
    - `jaeger`（Jaeger All-in-One）
    - `seq`（Seq）
    - `aspire-dashboard`（Aspire Dashboard）
  - 設定網路、端口映射、環境變數
  - 設定服務啟動依賴順序（depends_on）
  - 依賴：步驟 11、12、13

### 階段六：端對端驗證

- [x] **步驟 15：啟動並驗證完整鏈路**
  - 執行 `docker compose up -d`
  - 驗證項目：
    - [x] **前端介面**：開啟 http://localhost:3000，看到天氣介面
    - [x] **查詢天氣**：點擊「查詢天氣」，表格顯示由 backend-b 提供的天氣資料
    - [x] **新增天氣**：填寫表單並提交，確認新增成功並自動刷新列表
    - [x] **前端追蹤**：開啟瀏覽器 DevTools Network，確認 fetch 請求帶有 `traceparent` header
    - [x] **前端 OTLP**：確認瀏覽器有發送 OTLP HTTP 請求到 `http://localhost:4318/v1/traces`
    - [x] **後端端點**：backend-a http://localhost:5100/Weather 回傳資料
    - [x] **後端端點**：backend-b http://localhost:5200/Weather 回傳資料
    - [x] **完整鏈路**：Jaeger UI http://localhost:16686，搜尋 `frontend` service，可看到 frontend → backend-a → backend-b 完整 Trace
    - [x] **日誌系統**：Seq UI http://localhost:5341 可看到結構化日誌
    - [x] **整合儀表板**：Aspire Dashboard http://localhost:18888 可看到 Traces/Metrics/Logs
    - [x] **日誌輸出**：`docker compose logs` 可看到格式化的結構化日誌

## 驗收標準

1. **分散式追蹤**：從 Jaeger UI 可看到一個完整的 Trace 包含 Frontend → backend-a → backend-b 三個 Span
2. **前端追蹤**：瀏覽器 fetch 請求自動帶有 `traceparent` header，OTLP HTTP 請求成功送到 OTel Collector
3. **日誌關聯**：Seq 中的日誌記錄包含 TraceId，可與 Jaeger Trace 互相關聯
4. **Aspire Dashboard**：可同時查看 Traces、Metrics、Logs
5. **一鍵啟動**：`docker compose up -d` 即可啟動所有服務
6. **Console 日誌**：透過 `docker compose logs` 可看到格式化的結構化日誌
7. **天氣介面**：前端頁面可查詢天氣列表、新增天氣資料，並自動刷新
