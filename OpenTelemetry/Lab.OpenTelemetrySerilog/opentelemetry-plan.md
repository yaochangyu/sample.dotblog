# OpenTelemetry + Serilog 開發環境建置計畫

## 架構概覽

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Nuxt 4     │────▶│   backend-a      │────▶│   backend-b      │
│  (Frontend)  │     │ ASP.NET 10   │     │ ASP.NET 10   │
└──────────────┘     └──────┬───────┘     └──────┬───────┘
                            │                     │
                     Serilog│+ OTel SDK    Serilog│+ OTel SDK
                            │                     │
                            ▼                     ▼
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
Nuxt Frontend (HTTP Request)
  └──▶ API-A (接收請求，處理後呼叫 API-B)
         └──▶ API-B (處理請求，回傳結果)
```

### 資料流向

| 訊號類型 | 來源 | 傳輸方式 | 目的地 |
|---------|------|---------|--------|
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

### 階段四：業務邏輯與追蹤鏈路

- [ ] **步驟 8：實作 backend-a 端點**
  - 建立 `GET /api/weather` 端點
  - 該端點使用 `HttpClient` 呼叫 API-B 的 `GET /api/forecast`
  - 加入 Serilog 結構化日誌記錄
  - 依賴：步驟 6

- [ ] **步驟 9：實作 backend-b 端點**
  - 建立 `GET /api/forecast` 端點
  - 回傳模擬的天氣預報資料
  - 加入 Serilog 結構化日誌記錄
  - 依賴：步驟 7

- [ ] **步驟 10：實作 Nuxt 前端頁面**
  - 建立首頁，呼叫 API-A 的 `GET /api/weather`
  - 顯示回傳的天氣資料
  - 依賴：步驟 3

### 階段五：基礎建設 (Docker)

- [ ] **步驟 11：建立 OTel Collector 設定檔**
  - 建立 `otel-collector-config.yaml`
  - 設定 Receivers：OTLP (gRPC + HTTP)
  - 設定 Exporters：Jaeger (OTLP)、Aspire Dashboard (OTLP)
  - 設定 Pipelines：traces / metrics / logs

- [ ] **步驟 12：建立後端 Dockerfile**
  - 為 API-A 和 API-B 各建立 `Dockerfile`
  - 使用 multi-stage build（SDK build → Runtime image）
  - 驗證：`docker build` 成功

- [ ] **步驟 13：建立前端 Dockerfile**
  - 為 Nuxt 前端建立 `Dockerfile`
  - 驗證：`docker build` 成功

- [ ] **步驟 14：建立 docker-compose.yml**
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

- [ ] **步驟 15：啟動並驗證完整鏈路**
  - 執行 `docker compose up -d`
  - 驗證項目：
    - [ ] Nuxt 前端 http://localhost:3000 可存取
    - [ ] backend-a http://localhost:5100/api/weather 回傳資料
    - [ ] backend-b http://localhost:5200/api/forecast 回傳資料
    - [ ] 前端呼叫 → backend-a → API-B 完整鏈路正常
    - [ ] Jaeger UI http://localhost:16686 可看到跨服務 Trace
    - [ ] Seq UI http://localhost:5341 可看到結構化日誌
    - [ ] Aspire Dashboard http://localhost:18888 可看到 Traces / Metrics / Logs
    - [ ] Trace 中的 TraceId 在 backend-a → backend-b 之間正確傳播
    - [ ] Serilog 日誌包含 TraceId / SpanId 關聯資訊

## 驗收標準

1. **分散式追蹤**：從 Jaeger UI 可看到一個完整的 Trace 包含 Frontend → backend-a → backend-b 三個 Span
2. **日誌關聯**：Seq 中的日誌記錄包含 TraceId，可與 Jaeger Trace 互相關聯
3. **Aspire Dashboard**：可同時查看 Traces、Metrics、Logs
4. **一鍵啟動**：`docker compose up -d` 即可啟動所有服務
5. **Console 日誌**：透過 `docker compose logs` 可看到格式化的結構化日誌
