# 前端 OpenTelemetry 瀏覽器端追蹤 + 天氣介面實作計畫

## Context

目前 Nuxt 4 前端專案沒有任何 OpenTelemetry 設定，也沒有實際的業務頁面。後端 backend-a 已提供 `GET /Weather` 和 `POST /Weather` 端點，但前端仍停留在 NuxtWelcome 預設頁面。

本次實作目標：
1. 建立前端天氣介面，呼叫 backend-a 的 GET/POST /Weather 端點
2. 透過 Nuxt Server Proxy 代理 API 請求（同源，不需 CORS on backend-a）
3. 加入 OTel 瀏覽器端追蹤，自動 instrument fetch 並注入 `traceparent` header
4. `traceparent` 透過 Nuxt proxy 透傳到 backend-a，實現完整分散式追蹤

### 追蹤鏈路

```
Browser (OTel fetch span, 注入 traceparent)
  → Nuxt Server Proxy (/api/weather → backend-a:8080/Weather, 透傳 traceparent)
    → backend-a (讀取 traceparent, 建立 child span)
      → backend-b (讀取 traceparent, 建立 child span)
```

## 實作步驟

### 階段一：OpenTelemetry 套件安裝

- [x] **步驟 1：安裝 OpenTelemetry npm 套件**
  - 檔案：`src/frontend/package.json`
  - 安裝套件：
    - `@opentelemetry/api` — Core API
    - `@opentelemetry/sdk-trace-web` — 瀏覽器端 WebTracerProvider
    - `@opentelemetry/sdk-trace-base` — BatchSpanProcessor
    - `@opentelemetry/exporter-trace-otlp-http` — OTLP HTTP exporter
    - `@opentelemetry/instrumentation-fetch` — 自動 instrument fetch 請求
    - `@opentelemetry/instrumentation` — registerInstrumentations
    - `@opentelemetry/resources` — 設定 service name
    - `@opentelemetry/semantic-conventions` — ATTR_SERVICE_NAME
    - `@opentelemetry/context-zone` — ZoneContextManager（瀏覽器非同步 context 傳播）
  - 驗證：`npm install` 成功

### 階段二：OTel 瀏覽器端追蹤設定

- [x] **步驟 2：建立 Nuxt client-only plugin**
  - 新增檔案：`src/frontend/plugins/opentelemetry.client.ts`
  - 建立 `.client.ts` 後綴的 Nuxt plugin（只在瀏覽器端執行）
  - 內容：
    - `WebTracerProvider` — service name 為 `frontend`
    - `BatchSpanProcessor` + `OTLPTraceExporter` — 指向 OTel Collector OTLP HTTP 端點
    - `ZoneContextManager` — 瀏覽器非同步 context 傳播
    - `W3CTraceContextPropagator` — 自動注入 `traceparent` header
    - `FetchInstrumentation` — 自動 instrument fetch 請求（同源請求自動傳播 trace context）
    - 透過 `useRuntimeConfig().public.otelCollectorUrl` 讀取可配置的端點
  - 依賴：步驟 1

- [x] **步驟 3：更新 nuxt.config.ts**
  - 檔案：`src/frontend/nuxt.config.ts`
  - 新增 `runtimeConfig.public.otelCollectorUrl`（預設 `http://localhost:4318`）
  - 新增 `routeRules` 設定 Server Proxy：`/api/weather/**` → backend-a `/Weather`
  - 使用環境變數 `BACKEND_A_URL` 讓 proxy 目標可在 Docker 環境中配置
  - 依賴：步驟 2

### 階段三：前端天氣介面

- [x] **步驟 4：建立天氣介面頁面**
  - 修改檔案：`src/frontend/app/app.vue` — 替換 `NuxtWelcome` 為 `NuxtPage`
  - 新增檔案：`src/frontend/app/pages/index.vue`
  - 頁面功能：
    - **查詢天氣**：按鈕觸發 `GET /api/weather`，以表格顯示天氣預報列表（Date、TemperatureC、TemperatureF、Summary）
    - **新增天氣**：表單輸入 Date、TemperatureC、Summary，Submit 觸發 `POST /api/weather`，成功後自動重新載入天氣列表
  - 依賴：步驟 3

### 階段四：基礎建設更新

- [x] **步驟 5：更新 OTel Collector CORS 設定**
  - 檔案：`otel-collector-config.yaml`
  - 在 `receivers.otlp.protocols.http` 下新增 CORS 設定（瀏覽器發送 OTLP trace 到 Collector 需要）
  - 允許來源：`http://localhost:3000`
  - 允許 headers：`*`

### 階段五：文件更新

- [x] **步驟 6：更新 opentelemetry-plan.md**
  - 檔案：`opentelemetry-plan.md`
  - 在「階段三：OpenTelemetry SDK 整合」新增前端 OTel 步驟
  - 更新資料流向表格（新增 Frontend Traces 項目）
  - 更新驗收標準（新增前端→後端完整 Trace 驗證）

## 需修改的檔案

| 檔案 | 操作 |
|------|------|
| `src/frontend/package.json` | 新增 OTel 依賴（npm install） |
| `src/frontend/plugins/opentelemetry.client.ts` | **新增** — OTel 初始化 plugin |
| `src/frontend/nuxt.config.ts` | 新增 runtimeConfig + routeRules proxy |
| `src/frontend/app/app.vue` | 替換 NuxtWelcome 為 NuxtPage |
| `src/frontend/app/pages/index.vue` | **新增** — 天氣查詢/新增介面 |
| `otel-collector-config.yaml` | 新增 CORS 設定 |
| `opentelemetry-plan.md` | 新增前端 OTel 步驟與更新資料流向 |

## 驗證方式

- [ ] `docker compose up -d` 啟動所有服務
- [ ] 開啟 http://localhost:3000，看到天氣介面
- [ ] 點擊「查詢天氣」，表格顯示天氣資料
- [ ] 填寫表單並提交，確認新增成功並自動刷新列表
- [ ] 開啟瀏覽器 DevTools Network，確認 fetch 請求帶有 `traceparent` header
- [ ] 確認瀏覽器有發送 OTLP HTTP 請求到 `http://localhost:4318/v1/traces`
- [ ] 開啟 Jaeger UI http://localhost:16686，搜尋 `frontend` service，確認可看到 frontend → backend-a → backend-b 完整 Trace
