# 安全性測試計畫

## 測試目標

驗證 `/api/token` 與 `/api/protected` 的目前實作，確認：
1. Token 可以正確產生、過期並套用使用次數限制
2. Token 綁定 User-Agent，且受保護端點會驗證 User-Agent、Referer、Origin 與 Token
3. 速率限制與瀏覽器整合流程符合目前程式碼行為

## 測試環境設定

- **API 伺服器**: `http://localhost:5073` 或 `https://localhost:7026`
- **測試工具**:
  - curl（命令列測試）
  - PowerShell 腳本（Windows）
  - Bash 腳本（Linux/macOS）
  - Playwright（瀏覽器自動化測試）

## 目前測試案例總覽

本專案目前共有 **13 個測試案例**：

| 類別 | 數量 | 實際腳本 |
| --- | --- | --- |
| Token 基本功能 | 3 | `test-01` ~ `test-03` |
| 安全防護驗證 | 7 | `test-04`、`test-05`、`test-05-4`、`test-06`、`test-07`、`test-11`、`test-12` |
| 瀏覽器整合測試 | 3 | `test-08` ~ `test-10` |

## 測試案例

### Token 基本功能

#### 1. 正常流程
- **目的**: 驗證可先取得 Token，再成功呼叫 Protected API
- **預期結果**:
  - `GET /api/token` 回傳 `200 OK`
  - Response Header 含 `X-CSRF-Token`
  - `POST /api/protected` 回傳 `200 OK`
- **腳本**:
  - `scripts/test-01-normal-flow.sh`
  - `scripts/test-01-normal-flow.ps1`

#### 2. Token 過期
- **目的**: 驗證過期 Token 不能再使用
- **預期結果**:
  - 使用過期 Token 呼叫 `POST /api/protected` 時回傳 `401 Unauthorized`
  - 錯誤訊息為 `Invalid or expired token`
- **腳本**:
  - `scripts/test-02-token-expiration.sh`
  - `scripts/test-02-token-expiration.ps1`

#### 3. 使用次數限制
- **目的**: 驗證 Token 達到 `maxUsage` 後失效
- **預期結果**:
  - 首次呼叫成功
  - 再次使用同一個 Token 時回傳 `401 Unauthorized`
- **腳本**:
  - `scripts/test-03-usage-limit.sh`
  - `scripts/test-03-usage-limit.ps1`

### 安全防護驗證

#### 4. 缺少 Token
- **目的**: 驗證未提供 `X-CSRF-Token` 時會被拒絕
- **預期結果**:
  - 回傳 `401 Unauthorized`
- **腳本**:
  - `scripts/test-04-missing-token.sh`
  - `scripts/test-04-missing-token.ps1`

#### 5. 無效 Token
- **目的**: 驗證偽造或錯誤 Token 會被拒絕
- **預期結果**:
  - 回傳 `401 Unauthorized`
  - 錯誤訊息為 `Invalid or expired token`
- **腳本**:
  - `scripts/test-05-invalid-token.sh`
  - `scripts/test-05-invalid-token.ps1`

#### 5-4. 缺少 User-Agent
- **目的**: 驗證取得 Token 時未提供 User-Agent 會失敗
- **預期結果**:
  - `GET /api/token` 回傳 `400 Bad Request`
  - 錯誤訊息為 `User-Agent header is required`
- **腳本**:
  - `scripts/test-05-4-missing-user-agent.ps1`

#### 6. User-Agent 不一致
- **目的**: 驗證 Token 綁定申請時的 User-Agent
- **預期結果**:
  - 使用不同 User-Agent 呼叫 `POST /api/protected` 時回傳 `401 Unauthorized`
- **腳本**:
  - `scripts/test-06-ua-mismatch.sh`
  - `scripts/test-06-ua-mismatch.ps1`

#### 7. 速率限制
- **目的**: 驗證 Token 端點的速率限制
- **預期結果**:
  - 1 分鐘內前 5 次 `GET /api/token` 成功
  - 第 6 次回傳 `429 Too Many Requests`
- **腳本**:
  - `scripts/test-07-rate-limiting.sh`
  - `scripts/test-07-rate-limiting.ps1`

#### 11. 直接攻擊
- **目的**: 驗證未先取得 Token 直接呼叫 Protected API 會失敗
- **預期結果**:
  - 回傳 `401 Unauthorized` 或 `403 Forbidden`（若先被 User-Agent 黑名單擋下）
- **腳本**:
  - `scripts/test-11-direct-attack.sh`
  - `scripts/test-11-direct-attack.ps1`

#### 12. 重放攻擊
- **目的**: 驗證失效 Token 不可重複使用
- **預期結果**:
  - 首次請求成功
  - 再次使用相同 Token 時回傳 `401 Unauthorized`
- **腳本**:
  - `scripts/test-12-replay-attack.sh`
  - `scripts/test-12-replay-attack.ps1`

### 瀏覽器整合測試

#### 8. 瀏覽器正常流程
- **目的**: 驗證瀏覽器頁面可完成取得 Token 與呼叫 API 的完整流程
- **腳本**:
  - `scripts/test-08-browser-normal.spec.js`

#### 9. 瀏覽器使用次數限制
- **目的**: 驗證瀏覽器環境下的使用次數限制
- **腳本**:
  - `scripts/test-09-browser-usage-limit.spec.js`

#### 10. 跨頁面驗證
- **目的**: 驗證相同瀏覽器環境跨頁面使用 Token 的行為
- **腳本**:
  - `scripts/test-10-cross-page.spec.js`

## 測試執行

### 執行所有測試

```bash
# Linux/macOS
./scripts/run-all-tests.sh

# Windows PowerShell
.\scripts\run-all-tests.ps1
```

### 執行瀏覽器測試

```bash
npm install
npx playwright install
npx playwright test scripts/test-08-browser-normal.spec.js
```

## 已知限制與建議

1. **IP 綁定**: 目前 IP 驗證邏輯保留為可選，預設未啟用
2. **HTTPS**: 開發可用 HTTP，生產環境建議強制 HTTPS
3. **Token 儲存**: 使用 `IMemoryCache`，伺服器重啟後資料不保留
4. **Token 回應格式**: 目前同時透過 Response Header 與 JSON Body 回傳 Token
