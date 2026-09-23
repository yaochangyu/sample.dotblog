# Lab.Signature.WebApi

這是一個示範 **HMAC-SHA256 簽章保護 API 端點**的 ASP.NET Core 教學 Lab。它以外部合作夥伴呼叫 API（例如金流或 webhook 串接）為情境，示範如何用 API Key、Timestamp、Nonce 與 Signature 防止請求竄改與重放，並提供可在瀏覽器操作的 Client Demo 頁面。

> **重要限制**：本專案是教學示範，不是可直接部署到正式環境的完整方案。專案刻意使用明碼 Secret、單機記憶體 Nonce 快取與詳細的 401 錯誤訊息，完整風險請見[正式環境不適用的原因](#正式環境不適用的原因)。

## 架構

### 請求處理分層

```text
HTTP Request
    │
    ▼
SignatureAuthenticationMiddleware
    │ 只處理 /api/protected/*
    │ 驗證 Header、Timestamp、Canonical String、HMAC 與 Nonce
    ▼
OrdersController
    │ 薄 Controller，只處理 HTTP 輸入/輸出
    ▼
Handler / Service
    ├─ SignatureValidationHandler：組裝 Canonical String、計算並比對 HMAC-SHA256
    └─ MemoryCacheNonceStore：以 IMemoryCache 防止同一 ApiKey+Nonce 重複使用
    │
    ▼
Repository
    └─ ApiKeyClientRepository：從 SignatureDbContext 查詢 ApiKey 與 Secret
    │
    ▼
PostgreSQL
    └─ ApiKeyClients：保存教學用 ApiKey、Secret、ClientName 與 CreatedAt
```

- **Controller**：`OrdersController` 提供兩個受保護的示範端點，回傳假訂單資料，不保存訂單資料。
- **Handler**：`SignatureValidationHandler` 負責簽章驗證規則；`MemoryCacheNonceStore` 負責 Nonce 的原子消費。
- **Repository**：`ApiKeyClientRepository` 使用 EF Core 查詢 ApiKey 對應的合作夥伴 Secret。
- **Middleware**：只對 `/api/protected/*` 啟用驗證；`/swagger` 等非 protected 路徑直接放行。驗證失敗時教學模式會回傳 HTTP 401 與具體原因；正式環境應改回泛化錯誤。

### Middleware 驗證流程

1. 判斷請求是否位於 `/api/protected/*`；非受保護路徑直接交給後續 pipeline。
2. 讀取 `X-Api-Key`、`X-Timestamp`、`X-Nonce`、`X-Signature`。
3. **便宜檢查（不讀 body、不查 DB）**：必要 Header 是否齊全、是否帶了不允許的 Query String、Unix timestamp 格式/範圍是否合法、是否在伺服器時間 ±5 分鐘視窗內。任何一項失敗都會在這裡直接回 401，避免匿名攻擊者用大 body 消耗資源（DoS 防護）。
4. **Body Size 上限檢查**：`Content-Length` 若存在且超過 64KB 直接拒絕；讀取時也設有位元組數上限，防止 `Content-Length` 缺失或不實（例如 chunked transfer）時仍整包讀進記憶體。
5. 通過上述檢查後才啟用 request buffering，讀取原始 request body bytes，並將 stream rewind。
6. 依 `X-Api-Key` 從 Repository 查詢 Secret。
7. 以原始 body bytes 計算 body hash，組裝 Canonical String，計算 HMAC-SHA256 並以 constant-time comparison 比對。
8. 簽章成功後，以 `{ApiKey}:{Nonce}` 為 key 原子性消費 Nonce；已使用過的 Nonce 回傳 `NonceReused`。
9. 驗證成功後再次將 `Request.Body.Position` 設為 0，讓 Controller 能正常讀取 POST body。

## API 端點

| 方法 | 路徑 | 說明 |
| --- | --- | --- |
| `GET` | `/api/protected/orders/{id}` | 回傳教學用假訂單資料 |
| `POST` | `/api/protected/orders` | 接收 `productName` 與 `amount`，回傳建立結果 |

受保護端點必須帶以下四個 Header：

```text
X-Api-Key
X-Timestamp
X-Nonce
X-Signature
```

### Admin API：頒發 Key

Admin API 是供內部管理工具使用的核發與查詢功能，不是對外提供的自助申請或取得 Key
功能，也不套用 `SignatureAuthenticationMiddleware` 的 HMAC 簽章保護。它只在
Development 環境可用，呼叫時必須帶 `X-Admin-Key` Header，值為
`appsettings.Development.json` 中的 `AdminApiKey` 設定值。

路由本身在所有環境都會由 `MapControllers()` 註冊；非 Development 環境的請求會先由
`AdminAuthenticationMiddleware` 攔截並回傳 `404 Not Found`，讓端點對外表現得像不存在。
Development 環境下缺少或帶錯 `X-Admin-Key` 則回傳 `401 Unauthorized`。

| 方法 | 路徑 | 說明 |
| --- | --- | --- |
| `POST` | `/api/admin/clients` | Request Body 只需 `ClientName`；核發並回傳 `ApiKey`、`Secret`、`ClientName`、`CreatedAt`。`Secret` 只在這次回應顯示一次 |
| `GET` | `/api/admin/clients` | 回傳已核發 Client 清單，包含 `ApiKey`、`ClientName`、`CreatedAt`，不含 `Secret` |

核發範例（Development）：

```bash
curl -X POST http://localhost:5232/api/admin/clients \
  -H 'Content-Type: application/json' \
  -H 'X-Admin-Key: admin-dev-only-please-change' \
  -d '{"clientName":"Partner Alpha"}'
```

回應範例：

```json
{
  "apiKey": "key-<32碼隨機hex，範例省略>",
  "secret": "<64碼隨機hex，僅本次回應顯示，範例省略>",
  "clientName": "Partner Alpha",
  "createdAt": "2026-09-23T00:00:00+00:00"
}
```

查詢清單：

```bash
curl http://localhost:5232/api/admin/clients \
  -H 'X-Admin-Key: admin-dev-only-please-change'
```

也可以在 Development 環境開啟 Swagger UI（<http://localhost:5232/swagger>），於
Admin API 請求的 `X-Admin-Key` Header 填入設定值後操作。

## 延伸文件

- [doc/key-application-form-example.md](./doc/key-application-form-example.md)：API Key 申請單的空白表格與填寫範例
- [doc/key-application-process.md](./doc/key-application-process.md)：從提出申請、內部審核、Admin API 核發到安全交付的完整流程
- [doc/signature-generation-and-verification.md](./doc/signature-generation-and-verification.md)：Client 產生 HMAC 簽章與 Server 驗證／比對簽章的機制
- [doc/protected-api-verification.md](./doc/protected-api-verification.md)：受保護 API 請求從 Header 檢查到 Nonce 防重放的完整驗證流程

## 簽章機制規格

### Canonical String

Canonical String 使用換行分隔、逐欄固定格式：

```text
METHOD
PATH
TIMESTAMP
NONCE
API_KEY
BODY_SHA256_HEX
```

- `METHOD`：大寫（`GET`/`POST`）。
- `PATH`：request path，不含 scheme/host，保留原始 percent-encoding。
- **不支援 Query String**：兩個示範端點（`GET /orders/{id}`、`POST /orders`）都不使用 query 參數，Middleware 明確拒絕帶 query string 的 protected 請求，避免另外定義 canonical query 排序規則。
- `TIMESTAMP`：Unix epoch seconds，不與 ISO-8601 混用；必須落在伺服器時間前後 5 分鐘內。
- `BODY_SHA256_HEX`：對**原始 request body bytes**（不是重新序列化的 JSON）計算 SHA-256，輸出小寫 hex。GET 或空 body 固定使用空字串的 SHA-256 值。
- 六個欄位以 `\n` 分隔，欄位順序不可更動。

### HMAC 計算與比對

伺服器以 UTF-8 將 Secret 與 Canonical String 轉為 bytes，計算：

```text
HMAC-SHA256(key = UTF-8(Secret), message = UTF-8(Canonical String))
```

`X-Signature` 輸出格式固定為小寫 hex。伺服器重新計算後，以 `CryptographicOperations.FixedTimeEquals` 執行 constant-time comparison；非法 hex、長度不符或內容不符都視為 `SignatureMismatch`。Client Demo 使用瀏覽器 `crypto.subtle` 以相同規格計算簽章。

### Nonce 防重放

- Cache Key 為 `{ApiKey}:{Nonce}`，避免不同合作夥伴使用相同 Nonce 時互相碰撞。
- 必須先通過 ApiKey 存在、Timestamp 視窗與 HMAC 簽章驗證，才會標記 Nonce 已使用；無效請求不會污染 Nonce 空間。
- 教學版使用 per-key `SemaphoreSlim`，讓「檢查存在 → 標記已用」成為原子操作，避免同一 Nonce 的併發 replay 同時通過。
- Nonce 快取過期時間為 5 分鐘，與 Timestamp 視窗一致。
- `IMemoryCache` 僅適合單機教學。多實例部署或服務重啟會使防重放狀態遺失；正式環境應使用 Redis `SET NX EX` 或具 unique constraint 的資料庫方案。

## 本機啟動

需求：.NET 10 SDK、Docker，以及可用的 Docker daemon。

本專案提供 [Taskfile](https://taskfile.dev/)（`Taskfile.yml`）封裝常用指令。若已安裝 `task` CLI，可用 `task dev` 一次完成「啟動 PostgreSQL → 套用 Migration → 啟動 API」；也可以照下面步驟手動執行對應的原生指令，兩者等價。

### 1. 啟動 PostgreSQL

```bash
task db-up
# 等同於：docker compose up -d
```

`docker-compose.yml` 會啟動 PostgreSQL 16，資料庫名稱為 `lab_api_signature`，本機 port 為 `5432`。

### 2. 套用 EF Core Migration

如果尚未套用過 Migration：

```bash
task ef-database-update
# 等同於：cd src/Lab.Signature.WebApi && dotnet ef database update
```

Migration 會建立 `ApiKeyClients` 資料表，並加入三組僅供本機教學使用的 demo 憑證：

| ApiKey | Secret |
| --- | --- |
| `demo-api-key-001` | `demo-secret-001-please-change` |
| `demo-api-key-002` | `demo-secret-002-please-change` |
| `demo-api-key-003` | `demo-secret-003-please-change` |

這些值只可用於本機 Lab，不得替換成真實合作夥伴 Secret。

### 3. 啟動 API

```bash
task api-dev
# 等同於：cd src/Lab.Signature.WebApi && dotnet run
```

HTTP profile 預設監聽 `http://localhost:5232`。

### 常用 Task 指令

| 指令 | 說明 |
| --- | --- |
| `task dev` | 一鍵啟動：`db-up` → `ef-database-update` → `api-dev` |
| `task build` | 建置整個 solution |
| `task test` | 執行全部測試（單元 + BDD，BDD 需要 Docker） |
| `task test-unit` | 只執行單元測試（不需要 Docker） |
| `task test-integration` | 只執行 BDD 整合測試（Testcontainers 啟動 PostgreSQL） |
| `task db-up` / `task db-down` | 啟動 / 停止本機 PostgreSQL 容器 |
| `task ef-database-update` | 套用 EF Core Migration |
| `task ef-migration-add MIGRATION_NAME=AddXxx` | 新增 Migration |
| `task --list-all` | 列出所有可用指令與說明 |

### 4. 開啟 Client Demo

用瀏覽器開啟：

<http://localhost:5232/index.html>

在 Client Demo 頁面選擇 demo 憑證與 GET/POST 端點後即可送出正常請求。成功送出一次後，可以使用三個攻擊模擬按鈕觀察驗證結果：

- **竄改 Body（沿用舊簽章）**：Body hash 改變，應回傳 `SignatureMismatch`。
- **重送同一 Nonce（Replay）**：同一組 ApiKey+Nonce 再次使用，應回傳 `NonceReused`。
- **使用過期 Timestamp**：使用 6 分鐘前的 timestamp，應回傳 `TimestampExpired`。

Swagger UI 在 Development 環境可由 <http://localhost:5232/swagger> 開啟。

## Secret 風險揭露清單

本 Lab 為了讓 HMAC 驗證流程容易理解，將 Secret 以明碼存放，這是刻意的教學簡化。請完整注意以下風險：

- Migration Seed 只能用 demo secret，不得使用真實合作夥伴 secret
- Secret 不得輸出到 log、exception、401 response、Swagger 範例、git history
- `appsettings.json` / `docker-compose.yml` 不得放真實 secret
- DB backup/dump、開發者本機資料庫都等同暴露 secret
- `AdminApiKey` 明碼放在 `appsettings.Development.json`，預設值為
  `admin-dev-only-please-change`，僅供本機教學；正式環境需要正式的權限管控機制
- 正式環境至少要有 Secret Manager/KMS、存取控管、輪替、稽核、加密備份策略

## 正式環境不適用的原因

本專案不適合直接部署到正式環境，原因如下：

- Secret 明碼存放
- Nonce 單機記憶體
- 錯誤訊息過於詳細
- Client Demo 瀏覽器持有 Secret
- 無 rotation 機制
- 無 partner onboarding/offboarding 流程
- 無完整 audit log

正式方案至少需要把 Secret 移至受控的 Secret Manager/KMS、以跨實例共享且具原子語意的儲存機制處理 Nonce、泛化對外錯誤訊息並將細節寫入安全的 server log，再補足 Secret 輪替、合作夥伴生命週期與完整稽核能力。

## 執行測試

測試使用 Reqnroll BDD、`WebApplicationFactory` 與 Testcontainers PostgreSQL，會啟動真正的 PostgreSQL container，因此本機必須有 Docker 支援。

在本目錄執行：

```bash
task test           # 全部測試（單元 + BDD），等同於 dotnet test
task test-unit       # 只跑單元測試，不需要 Docker
task test-integration # 只跑 BDD 整合測試
```

測試涵蓋正常 GET/POST、Body 竄改、Replay、過期 Timestamp、未知 ApiKey、缺少 Header、錯誤格式、Query String、空 body hash、不同 ApiKey 共用 Nonce、同一 Nonce 的併發請求，以及 Middleware 便宜檢查是否真的在讀 body 之前就攔截、Payload 超過大小上限等安全邊界。
