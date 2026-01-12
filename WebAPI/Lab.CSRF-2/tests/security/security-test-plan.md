# 安全性測試計畫

## 測試目標

驗證 `/api/protected` 端點的安全防護機制，確保：
1. 只能被當前頁面使用（User-Agent 綁定）
2. 防止被爬蟲濫用（Token 使用次數限制、速率限制）
3. 在 curl 直接請求時無法繞過安全機制

## 測試環境設定

- **API 伺服器**: http://localhost:5073 或 https://localhost:7001
- **測試工具**: 
  - curl (命令列測試)
  - PowerShell 腳本 (Windows)
  - Bash 腳本 (Linux/macOS)
  - Playwright (瀏覽器自動化測試)

## 測試場景

### 場景 1: Token 基本功能驗證

#### 測試案例 1.1: 正常 Token 取得與使用
**目的**: 驗證正常流程可以正常運作

**測試步驟**:
1. 呼叫 `GET /api/token` 取得 Token
2. 使用 Token 呼叫 `POST /api/protected`
3. 驗證回應狀態為 200 OK

**預期結果**: 
- Token 成功取得
- Protected API 呼叫成功
- 回應包含處理結果

**測試腳本**:
- `scripts/test-01-normal-flow.sh`
- `scripts/test-01-normal-flow.ps1`

---

#### 測試案例 1.2: Token 過期測試
**目的**: 驗證過期的 Token 無法使用

**測試步驟**:
1. 取得 Token (設定 1 分鐘過期)
2. 等待 61 秒
3. 使用過期 Token 呼叫 Protected API

**預期結果**: 
- HTTP 403 Forbidden
- 錯誤訊息: "Invalid or expired token"

**測試腳本**:
- `scripts/test-02-token-expiration.sh`
- `scripts/test-02-token-expiration.ps1`

---

#### 測試案例 1.3: Token 使用次數限制與重放攻擊防護
**目的**: 驗證 Token 使用次數超過限制後失效，防止 Token 被重複使用（重放攻擊）

**測試步驟**:
1. 取得 Token (設定最大使用次數 = 1)
2. 第一次呼叫 Protected API (應成功)
3. 第二次呼叫 Protected API，使用相同 Token (應失敗，模擬重放攻擊)

**預期結果**:
- 第一次: HTTP 200 OK
- 第二次: HTTP 403 Forbidden
- 錯誤訊息: "Invalid or expired token" 或 "Token usage limit exceeded"

**安全意義**: 防止攻擊者攔截 Token 後重複使用

**測試腳本**:
- `scripts/test-03-usage-limit.sh`
- `scripts/test-03-usage-limit.ps1`

---

### 場景 2: 安全防護驗證

#### 測試案例 2.1: 無 Token 請求
**目的**: 驗證缺少 Token 的請求被拒絕

**測試步驟**:
1. 直接呼叫 `POST /api/protected` 不帶 `X-CSRF-Token` Header

**預期結果**: 
- HTTP 403 Forbidden
- 錯誤訊息: "Token is required"

**測試腳本**:
- `scripts/test-04-missing-token.sh`
- `scripts/test-04-missing-token.ps1`

---

#### 測試案例 2.2: 無效 Token
**目的**: 驗證偽造或錯誤的 Token 被拒絕

**測試步驟**:
1. 使用隨機產生的 GUID 作為 Token
2. 呼叫 Protected API

**預期結果**: 
- HTTP 403 Forbidden
- 錯誤訊息: "Invalid or expired token"

**測試腳本**:
- `scripts/test-05-invalid-token.sh`
- `scripts/test-05-invalid-token.ps1`

---

#### 測試案例 2.3: User-Agent 不一致
**目的**: 驗證 User-Agent 綁定機制，防止 Token 被盜用

**測試步驟**:
1. 使用 User-Agent A 取得 Token
2. 使用 User-Agent B 呼叫 Protected API

**預期結果**: 
- HTTP 403 Forbidden
- Token 驗證失敗 (User-Agent 不符)

**測試腳本**:
- `scripts/test-06-ua-mismatch.sh`
- `scripts/test-06-ua-mismatch.ps1`

---

#### 測試案例 2.4: 爬蟲模擬測試
**目的**: 驗證速率限制防止爬蟲濫用

**測試步驟**:
1. 快速連續發送多個 Token 請求 (超過速率限制)
2. 驗證速率限制生效

**預期結果**: 
- 前 5 次請求成功 (1 分鐘內最多 5 個 Token)
- 第 6 次請求: HTTP 429 Too Many Requests

**測試腳本**:
- `scripts/test-07-rate-limiting.sh`
- `scripts/test-07-rate-limiting.ps1`

---

#### 測試案例 2.5: 特殊字符注入測試
**目的**: 驗證系統對惡意輸入的防護能力

**測試步驟**:
1. 使用包含特殊字符的 Token（如 `'; DROP TABLE--`、`<script>alert('XSS')</script>`）
2. 呼叫 Protected API

**預期結果**:
- HTTP 403 Forbidden
- 系統不應受到 SQL Injection 或 XSS 攻擊影響
- 錯誤訊息: "Invalid or expired token"

**測試腳本**:
- `scripts/test-08-injection-attack.sh`
- `scripts/test-08-injection-attack.ps1`

---

#### 測試案例 2.6: HTTP Method 限制測試
**目的**: 驗證 Protected API 僅接受指定的 HTTP Method

**測試步驟**:
1. 取得有效 Token
2. 使用 GET 方法呼叫 `GET /api/protected`（應該是 POST）

**預期結果**:
- HTTP 405 Method Not Allowed 或 404 Not Found
- 只接受 POST 方法

**測試腳本**:
- `scripts/test-09-method-validation.sh`
- `scripts/test-09-method-validation.ps1`

---

#### 測試案例 2.7: Content-Type 驗證
**目的**: 驗證 API 正確處理錯誤的 Content-Type

**測試步驟**:
1. 取得有效 Token
2. 使用錯誤的 Content-Type（如 `text/plain`）呼叫 Protected API
3. 驗證 API 是否拒絕或容忍該請求

**預期結果**:
- HTTP 415 Unsupported Media Type（嚴格模式）或 HTTP 200 OK（寬鬆模式）
- 確認 API 的 Content-Type 處理策略

**測試腳本**:
- `scripts/test-10-content-type.sh`
- `scripts/test-10-content-type.ps1`

---

### 場景 3: 瀏覽器整合測試

#### 測試案例 3.1: 瀏覽器正常流程
**目的**: 驗證瀏覽器環境下的完整流程

**測試步驟** (使用 Playwright):
1. 開啟測試頁面 `http://localhost:5073/test.html`
2. 點擊「取得 Token」按鈕
3. 點擊「呼叫 Protected API」按鈕
4. 驗證顯示成功訊息

**預期結果**: 
- Token 成功取得並顯示在頁面
- API 呼叫成功並顯示回應資料

**測試腳本**:
- `scripts/test-11-browser-normal.spec.js` (Playwright)

---

#### 測試案例 3.2: 瀏覽器多次使用測試
**目的**: 驗證瀏覽器環境下的使用次數限制

**測試步驟**:
1. 開啟測試頁面
2. 設定最大使用次數 = 2
3. 取得 Token
4. 連續呼叫 3 次 Protected API

**預期結果**: 
- 前 2 次成功
- 第 3 次失敗 (403 Forbidden)

**測試腳本**:
- `scripts/test-12-browser-usage-limit.spec.js` (Playwright)

---

#### 測試案例 3.3: 跨頁面 Token 使用（相同瀏覽器）
**目的**: 驗證 Token 在相同瀏覽器的不同頁面間可正常使用（因為 User-Agent 相同）

**測試步驟**:
1. 頁面 A 取得 Token
2. 將 Token 複製到頁面 B（相同瀏覽器實例）
3. 頁面 B 使用該 Token 呼叫 Protected API

**預期結果**:
- HTTP 200 OK（因為 User-Agent 相同）
- 此測試驗證 User-Agent 綁定機制的正確性
- Token 應該可以在相同 User-Agent 的環境下使用

**測試腳本**:
- `scripts/test-13-cross-page.spec.js` (Playwright)

---

### 場景 4: 直接 curl 攻擊測試

#### 測試案例 4.1: 直接攻擊 Protected API
**目的**: 驗證不經過 Token 流程直接呼叫 API 失敗

**測試步驟**:
1. 直接使用 curl 呼叫 `POST /api/protected`
2. 不帶任何 Token

**預期結果**: 
- HTTP 403 Forbidden

**測試腳本**:
- `scripts/test-14-direct-attack.sh`
- `scripts/test-14-direct-attack.ps1`

---

#### 測試案例 4.2: 並發請求攻擊
**目的**: 驗證多個請求同時使用相同 Token 時的處理機制

**測試步驟**:
1. 取得 Token (maxUsage=3)
2. 同時發送 5 個並發請求使用相同 Token
3. 驗證只有 3 個請求成功

**預期結果**:
- 前 3 個請求: HTTP 200 OK
- 後 2 個請求: HTTP 403 Forbidden
- 確保使用次數計數的執行緒安全性

**測試腳本**:
- `scripts/test-15-concurrent-attack.sh`
- `scripts/test-15-concurrent-attack.ps1`

---

### 場景 5: 邊界條件測試

#### 測試案例 5.1: 空字串 Token
**目的**: 驗證系統正確處理空 Token

**測試步驟**:
1. 呼叫 Protected API，傳送空字串作為 Token (`X-CSRF-Token: ""`)
2. 驗證系統回應

**預期結果**:
- HTTP 403 Forbidden
- 錯誤訊息: "Token is required" 或 "Invalid or expired token"

**測試腳本**:
- `scripts/test-16-empty-token.sh`
- `scripts/test-16-empty-token.ps1`

---

#### 測試案例 5.2: 超長 Token
**目的**: 驗證系統對超長 Token 的處理

**測試步驟**:
1. 使用超長字串（如 10000 字符）作為 Token
2. 呼叫 Protected API

**預期結果**:
- HTTP 403 Forbidden 或 HTTP 400 Bad Request
- 系統不應崩潰或產生例外

**測試腳本**:
- `scripts/test-17-long-token.sh`
- `scripts/test-17-long-token.ps1`

---

#### 測試案例 5.3: Token 格式錯誤
**目的**: 驗證系統對非 GUID 格式 Token 的處理

**測試步驟**:
1. 使用非 GUID 格式的 Token（如 `abc123`、`not-a-guid`）
2. 呼叫 Protected API

**預期結果**:
- HTTP 403 Forbidden
- 錯誤訊息: "Invalid or expired token"

**測試腳本**:
- `scripts/test-18-malformed-token.sh`
- `scripts/test-18-malformed-token.ps1`

---

#### 測試案例 5.4: 缺少 User-Agent Header
**目的**: 驗證系統對缺少 User-Agent 的請求處理

**測試步驟**:
1. 取得 Token 時不傳送 User-Agent Header
2. 或使用 Token 時不傳送 User-Agent Header

**預期結果**:
- Token 取得可能成功或失敗（取決於實作）
- 使用 Token 時應失敗: HTTP 403 Forbidden

**測試腳本**:
- `scripts/test-19-missing-ua.sh`
- `scripts/test-19-missing-ua.ps1`

---

## 測試執行

### 快速測試（所有基本場景）
```bash
# Linux/macOS
./scripts/run-all-tests.sh

# Windows PowerShell
.\scripts\run-all-tests.ps1
```

### 個別測試
```bash
# Linux/macOS
./scripts/test-01-normal-flow.sh

# Windows PowerShell
.\scripts\test-01-normal-flow.ps1
```

### 瀏覽器測試
```bash
# 安裝 Playwright (首次執行)
npm install
npx playwright install

# 執行瀏覽器測試
npx playwright test scripts/test-08-browser-normal.spec.js
```

---

## 測試結果記錄

| 測試案例 | 狀態 | 備註 |
|---------|------|------|
| 1.1 正常流程 | ⏳ 待測試 | |
| 1.2 Token 過期 | ⏳ 待測試 | |
| 1.3 使用次數限制與重放攻擊防護 | ⏳ 待測試 | |
| 2.1 無 Token | ⏳ 待測試 | |
| 2.2 無效 Token | ⏳ 待測試 | |
| 2.3 User-Agent 不一致 | ⏳ 待測試 | |
| 2.4 速率限制 | ⏳ 待測試 | |
| 2.5 特殊字符注入測試 | ⏳ 待測試 | |
| 2.6 HTTP Method 限制測試 | ⏳ 待測試 | |
| 2.7 Content-Type 驗證 | ⏳ 待測試 | |
| 3.1 瀏覽器正常流程 | ⏳ 待測試 | |
| 3.2 瀏覽器使用限制 | ⏳ 待測試 | |
| 3.3 跨頁面使用（相同瀏覽器） | ⏳ 待測試 | |
| 4.1 直接攻擊 | ⏳ 待測試 | |
| 4.2 並發請求攻擊 | ⏳ 待測試 | |
| 5.1 空字串 Token | ⏳ 待測試 | |
| 5.2 超長 Token | ⏳ 待測試 | |
| 5.3 Token 格式錯誤 | ⏳ 待測試 | |
| 5.4 缺少 User-Agent Header | ⏳ 待測試 | |

---

## 安全性檢查清單

- [x] Token 必須綁定 User-Agent
- [x] Token 有明確的過期時間
- [x] Token 有使用次數限制
- [x] 受保護端點必須驗證 Token
- [x] 無效 Token 必須被拒絕
- [x] 實作速率限制防止暴力攻擊
- [ ] HTTPS 強制使用 (生產環境)
- [x] CORS 限制允許的來源
- [ ] 記錄可疑的 API 存取行為

---

## 已知限制與建議

1. **IP 綁定**: 目前 IP 檢查已註解，建議生產環境啟用
2. **HTTPS**: 測試環境使用 HTTP，生產環境必須使用 HTTPS
3. **Token 儲存**: 使用 Memory Cache，伺服器重啟會遺失，建議生產環境使用 Redis
4. **日誌監控**: 建議加入更詳細的安全事件日誌
5. **自動化測試**: 建議整合至 CI/CD 流程

## 測試報告輸出位置
- 資料夾 .\output\
- 總結 .\output\security-test-report.md