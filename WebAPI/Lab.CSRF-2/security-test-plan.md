# CSRF 安全測試計畫 - 完整版

## 📋 測試目標

確保 `api/protected` 端點的完整安全性，涵蓋以下防護面向：
- 跨站請求偽造 (CSRF) 防護
- Token 洩漏濫用防護
- 爬蟲與自動化攻擊防護
- 前端整合安全性驗證

---

## 📁 專案結構

```
Lab.CSRF-2/
├── tests/                             # 測試資料夾
│   └── security/                      # 安全測試
│       ├── scripts/                   # 測試腳本
│       │   ├── api-protected-security-test.ps1    # API 安全測試 (PowerShell)
│       │   ├── curl-security-test.ps1             # cURL 測試 (PowerShell)
│       │   ├── curl-security-test.sh              # cURL 測試 (Bash)
│       │   └── frontend-security-test.ps1         # 前端安全測試 (PowerShell)
│       ├── playwright/                # Playwright 測試
│       │   ├── api-protected.spec.ts  # API 測試規格
│       │   ├── csrf-protection.spec.ts # CSRF 防護測試
│       │   └── ...                    # 其他測試檔案
│       └── fixtures/                  # 測試頁面與資料
│           ├── test.html              # 測試頁面
│           └── api-protected-test.html # API 測試頁面
├── docs/                              # 文件資料夾
│   ├── api-protected-security-test-plan.md    # API 安全測試計畫
│   ├── curl-security-test-plan.md             # cURL 測試計畫
│   ├── frontend-playwright-test-plan.md       # Playwright 測試計畫
│   └── frontend-security-test-plan.md         # 前端安全測試計畫
├── security-test-plan.md              # 本文件 (整合測試計畫)
├── Lab.CSRF2.WebAPI/                  # WebAPI 專案
└── playwright.config.ts               # Playwright 設定檔
```

---

## 🚀 快速開始

### 執行所有測試

#### 方法 1: PowerShell 自動化測試 (推薦)

```powershell
# 1. 啟動 WebAPI 服務
cd Lab.CSRF2.WebAPI
dotnet run

# 2. 開啟新終端，執行 API 安全測試
cd tests/security/scripts
.\api-protected-security-test.ps1

# 3. 執行 cURL 測試
.\curl-security-test.ps1

# 4. 執行前端安全測試
.\frontend-security-test.ps1
```

#### 方法 2: Bash 腳本測試 (Linux/macOS)

```bash
# 1. 啟動 WebAPI 服務
cd Lab.CSRF2.WebAPI
dotnet run &

# 2. 執行 cURL 測試
cd ../tests/security/scripts
chmod +x curl-security-test.sh
./curl-security-test.sh
```

#### 方法 3: Playwright 前端測試

```bash
# 1. 安裝 Playwright (首次執行)
npm install
npx playwright install

# 2. 執行所有 Playwright 測試
npx playwright test

# 3. 檢視測試報告
npx playwright show-report
```

---

## 🎯 角色定位

- **API 端點名稱**: `api/protected`
- **存取權限**: 公開且可匿名使用
- **安全目標**:
  - 僅供當前頁面呼叫
  - 防止 CSRF 攻擊
  - Token 洩漏後仍能防止濫用
  - 降低爬蟲濫用風險

---

## 🧪 測試環境需求

### 基礎設施
- **作業系統**: Windows / Linux / macOS
- **API 服務**: ASP.NET Core (https://localhost:7001 或 https://localhost:5001)
- **瀏覽器**: Chrome / Edge / Firefox (含開發者工具)
- **Node.js**: 18.x 或以上 (Playwright 測試用)

### 工具清單
- **cURL**: 命令列 HTTP 請求測試
- **PowerShell 7+** 或 **Bash**: 自動化腳本執行
- **Playwright**: 前端自動化測試框架
- **Git Bash** (Windows): 執行 Shell 腳本

---

## 📊 測試項目分類

### 一、CSRF 防護測試 (7 項)

驗證 API 端點僅能被合法來源呼叫，拒絕跨站偽造請求。

#### TC-CSRF-01: 無 Token 的請求
- **測試方法**: 直接呼叫 `POST /api/protected`，不帶任何 Token Header
- **預期結果**: `401 Unauthorized` 或 `403 Forbidden`
- **驗證重點**: API 必須拒絕無 Token 請求
- **cURL 測試**:
  ```bash
  curl -X POST https://localhost:7001/api/protected \
    -H "Content-Type: application/json" \
    -d '{"data":"測試資料"}' \
    -k -i
  ```

#### TC-CSRF-02: 偽造 Token 的請求
- **測試方法**: 呼叫 `POST /api/protected`，帶自行產生的假 Token
- **預期結果**: `401 Unauthorized` 或 `403 Forbidden`
- **驗證重點**: 伺服器能識別並拒絕無效 Token
- **cURL 測試**:
  ```bash
  curl -X POST https://localhost:7001/api/protected \
    -H "Content-Type: application/json" \
    -H "X-CSRF-Token: fake-invalid-token-12345" \
    -d '{"data":"測試資料"}' \
    -k -i
  ```

#### TC-CSRF-03: 過期 Token 的請求
- **測試方法**:
  1. 取得有效 Token
  2. 等待 Token 過期（超過設定時間）
  3. 使用過期 Token 呼叫 API
- **預期結果**: `401 Unauthorized` 或 `403 Forbidden`
- **驗證重點**: Token 時效性管控
- **cURL 測試**:
  ```bash
  # 步驟 1: 取得短效 Token (1 秒過期)
  EXPIRED_TOKEN=$(curl -X GET "https://localhost:7001/api/token?maxUsage=5&expirationMinutes=0.016" \
    -k -i -s | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

  # 步驟 2: 等待 Token 過期
  sleep 2

  # 步驟 3: 使用過期 Token
  curl -X POST https://localhost:7001/api/protected \
    -H "Content-Type: application/json" \
    -H "X-CSRF-Token: $EXPIRED_TOKEN" \
    -d '{"data":"測試過期Token"}' \
    -k -i
  ```

#### TC-CSRF-04: 重複使用 Token (超過次數限制)
- **測試方法**:
  1. 取得有效 Token (maxUsage=2)
  2. 重複使用相同 Token 呼叫 API
- **預期結果**:
  - 前 2 次成功 (`200 OK`)
  - 第 3 次失敗 (`401 Unauthorized`)
- **驗證重點**: Token 使用次數限制生效
- **cURL 測試**:
  ```bash
  # 取得 Token
  TOKEN=$(curl -X GET "https://localhost:7001/api/token?maxUsage=2&expirationMinutes=5" \
    -k -i -s | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

  # 第一次使用 (成功)
  curl -X POST https://localhost:7001/api/protected \
    -H "X-CSRF-Token: $TOKEN" -d '{"data":"第一次"}' -k -i

  # 第二次使用 (成功)
  curl -X POST https://localhost:7001/api/protected \
    -H "X-CSRF-Token: $TOKEN" -d '{"data":"第二次"}' -k -i

  # 第三次使用 (失敗)
  curl -X POST https://localhost:7001/api/protected \
    -H "X-CSRF-Token: $TOKEN" -d '{"data":"第三次"}' -k -i
  ```

#### TC-CSRF-05: 跨域請求驗證 (CORS)
- **測試方法**: 從不同網域的網頁發起請求
- **預期結果**:
  - 若未設定 CORS，瀏覽器應阻擋請求
  - 若設定 CORS，僅允許白名單網域
- **驗證重點**: CORS 政策正確設定
- **Playwright 測試**: 參考 `TC-PW-CSRF-02`

#### TC-CSRF-06: Referer Header 驗證
- **測試方法**:
  1. 正常請求帶正確 Referer
  2. 偽造請求帶錯誤或空 Referer
- **預期結果**:
  - 正確 Referer → `200 OK`
  - 錯誤/空 Referer → `403 Forbidden`
- **驗證重點**: Referer 檢查機制（可選）
- **cURL 測試**:
  ```bash
  curl -X POST https://localhost:7001/api/protected \
    -H "X-CSRF-Token: $TOKEN" \
    -H "Referer: https://localhost:7001/" \
    -H "Content-Type: application/json" \
    -d '{"data":"test"}' -k -i
  ```

#### TC-CSRF-07: Origin Header 驗證
- **測試方法**:
  1. 正常請求帶正確 Origin
  2. 偽造請求帶錯誤 Origin
- **預期結果**:
  - 正確 Origin → `200 OK`
  - 錯誤 Origin → `403 Forbidden`
- **驗證重點**: Origin 檢查機制
- **cURL 測試**:
  ```bash
  curl -X POST https://localhost:7001/api/protected \
    -H "X-CSRF-Token: $TOKEN" \
    -H "Origin: https://localhost:7001" \
    -H "Content-Type: application/json" \
    -d '{"data":"test"}' -k -i
  ```

---

### 二、Token 洩漏防護測試 (4 項)

即使 Token 被洩漏，仍能透過其他機制防止濫用。

#### TC-LEAK-01: cURL 直接使用洩漏的 Token
- **測試方法**:
  1. 從瀏覽器正常取得 Token
  2. 使用 cURL 攜帶該 Token 發送請求
- **預期結果**:
  - 若有 User-Agent 檢查 → `403 Forbidden`
  - 若有 Referer/Origin 檢查 → `403 Forbidden`
  - 若僅依賴 Token → `200 OK`（有風險）
- **驗證重點**: Token 單獨驗證不足，需搭配其他檢查

#### TC-LEAK-02: Token 攔截後批次請求
- **測試方法**:
  1. 取得有效 Token
  2. 在短時間內發送大量請求（使用腳本）
- **預期結果**:
  - 觸發速率限制 → `429 Too Many Requests`
  - 或因次數限制快速耗盡 Token → `401 Unauthorized`
- **驗證重點**: 速率限制 (Rate Limiting) 機制
- **cURL 測試**:
  ```bash
  # 發送 100 次請求
  for i in {1..100}; do
    curl -X POST https://localhost:7001/api/protected \
         -H "X-CSRF-Token: $TOKEN" \
         -H "Content-Type: application/json" \
         -d '{"data":"test"}' -k &
  done
  wait
  ```

#### TC-LEAK-03: Token 在不同 IP 使用
- **測試方法**:
  1. 在 IP_A 取得 Token
  2. 在 IP_B 使用該 Token
- **預期結果**:
  - 若有 IP 綁定 → `403 Forbidden`
  - 若無 IP 綁定 → `200 OK`（較低安全性）
- **驗證重點**: IP 綁定機制（可選）

#### TC-LEAK-04: Token 在不同 User-Agent 使用
- **測試方法**:
  1. 使用瀏覽器取得 Token
  2. 使用 cURL (不同 User-Agent) 呼叫 API
- **預期結果**:
  - 若有 User-Agent 驗證 → `403 Forbidden`
  - 若無驗證 → `200 OK`
- **驗證重點**: User-Agent 一致性檢查
- **cURL 測試**:
  ```bash
  curl -X POST https://localhost:7001/api/protected \
       -H "X-CSRF-Token: $TOKEN" \
       -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)" \
       -H "Content-Type: application/json" \
       -d '{"data":"test"}' -k -i
  ```

---

### 三、爬蟲防護測試 (6 項)

降低自動化工具、爬蟲程式濫用 API 的風險。

#### TC-BOT-01: 無 User-Agent 的請求
- **測試方法**: 發送請求時移除 User-Agent Header
- **預期結果**: `403 Forbidden`
- **驗證重點**: 拒絕無 User-Agent 的請求

#### TC-BOT-02: 可疑 User-Agent 的請求
- **測試方法**: 使用已知爬蟲 User-Agent
  - `curl/7.68.0`
  - `python-requests/2.28.0`
  - `Wget/1.21`
- **預期結果**: `403 Forbidden`
- **驗證重點**: User-Agent 黑名單機制

#### TC-BOT-03: 高頻率請求 (速率限制)
- **測試方法**: 在短時間內發送大量請求（1 秒內 100 次）
- **預期結果**:
  - 前 N 次正常回應
  - 超過閾值後回傳 `429 Too Many Requests`
- **驗證重點**: 速率限制生效

#### TC-BOT-04: Token 生成頻率限制
- **測試方法**: 頻繁請求 `GET /api/token` (1 秒內 50 次)
- **預期結果**:
  - 前 N 次正常回傳 Token
  - 超過閾值後回傳 `429 Too Many Requests`
- **驗證重點**: Token 生成不應被濫用

#### TC-BOT-05: JavaScript 挑戰 (可選)
- **測試方法**:
  1. 純 cURL 請求 (無法執行 JavaScript)
  2. 真實瀏覽器請求 (可執行 JavaScript)
- **預期結果**:
  - cURL → 需額外驗證才能取得 Token
  - 瀏覽器 → 正常取得 Token
- **驗證重點**: JavaScript 驗證機制

#### TC-BOT-06: Honeypot 陷阱欄位
- **測試方法**:
  1. 在請求中填入隱藏欄位 (正常用戶不會填寫)
  2. 正常請求不帶隱藏欄位
- **預期結果**:
  - 帶隱藏欄位 → `403 Forbidden`
  - 不帶隱藏欄位 → `200 OK`
- **驗證重點**: Honeypot 機制識別爬蟲

---

### 四、前端整合測試 - Playwright (18 項)

使用 Playwright 自動化測試前端頁面與 API 的整合。

#### 4.1 Token 測試 (3 項)

##### TC-PW-TOKEN-01: 正常取得並使用 Token
- **測試步驟**:
  1. 使用 Playwright 開啟測試頁面
  2. 呼叫 `GET /api/token` 取得 Token
  3. 使用 Token 呼叫 `POST /api/protected`
  4. 驗證回應為 `200 OK`
- **驗證重點**:
  - Token 正確存在於 Response Header
  - API 呼叫成功
  - Referer/Origin 自動帶入

##### TC-PW-TOKEN-02: Token 過期後重新取得
- **測試步驟**:
  1. 取得 Token
  2. 等待 Token 過期
  3. 使用過期 Token 呼叫 API → `401 Unauthorized`
  4. 重新取得新 Token
  5. 使用新 Token 成功呼叫 API
- **驗證重點**: Token 時效性管控

##### TC-PW-TOKEN-03: Token 使用次數限制
- **測試步驟**:
  1. 取得 Token
  2. 迴圈呼叫 API（使用相同 Token）
  3. 記錄成功次數與失敗時的回應
- **驗證重點**:
  - 前 N 次成功
  - 第 N+1 次回傳 `401 Unauthorized`

#### 4.2 CSRF 防護測試 (3 項)

##### TC-PW-CSRF-01: 同源請求成功
- **測試步驟**:
  1. 從 `https://localhost:5001/test-page.html` 發起請求
  2. JavaScript fetch 呼叫 `POST /api/protected`
  3. 驗證 Referer/Origin 為 `https://localhost:5001`
  4. 驗證回應 `200 OK`
- **驗證重點**: 同源請求允許通過

##### TC-PW-CSRF-02: 跨域請求被阻擋
- **測試步驟**:
  1. 啟動另一個測試伺服器於 `http://localhost:3000`
  2. 從 `http://localhost:3000/attacker-page.html` 發起請求
  3. 嘗試呼叫 `https://localhost:5001/api/protected`
  4. 驗證瀏覽器 CORS 錯誤或 API 回傳 `403 Forbidden`
- **驗證重點**: CORS 政策阻擋跨域請求

##### TC-PW-CSRF-03: 偽造表單提交
- **測試步驟**:
  1. 建立惡意頁面 `attacker-page.html`
  2. 頁面包含自動提交表單，目標為 `/api/protected`
  3. 使用 Playwright 載入惡意頁面
  4. 驗證請求被拒絕
- **驗證重點**: 缺少 Token → `401 Unauthorized`

#### 4.3 Header 驗證測試 (3 項)

##### TC-PW-HEADER-01: Referer Header 自動帶入
- **測試步驟**:
  1. 從測試頁面發起 API 請求
  2. 使用 Playwright 攔截網路請求
  3. 驗證請求 Header 包含 `Referer: https://localhost:5001/`
- **驗證重點**: 瀏覽器自動帶入 Referer

##### TC-PW-HEADER-02: Origin Header 自動帶入
- **測試步驟**:
  1. 從測試頁面發起跨域 API 請求
  2. 攔截請求並驗證 `Origin` Header
- **驗證重點**: 瀏覽器自動帶入 Origin

##### TC-PW-HEADER-03: User-Agent 正常瀏覽器值
- **測試步驟**:
  1. 攔截 API 請求
  2. 驗證 User-Agent 為瀏覽器值（非 cURL）
- **驗證重點**: User-Agent 檢查機制能區分瀏覽器與腳本

#### 4.4 多瀏覽器測試 (3 項)

- **TC-PW-BROWSER-01**: Chromium (Chrome/Edge)
- **TC-PW-BROWSER-02**: Firefox
- **TC-PW-BROWSER-03**: WebKit (Safari)

**驗證重點**: 所有主流瀏覽器行為一致

#### 4.5 JavaScript 環境 (2 項)

##### TC-PW-JS-01: JavaScript 必須啟用
- **測試步驟**:
  1. 使用 Playwright 停用 JavaScript
  2. 嘗試存取測試頁面
  3. 驗證無法取得 Token 或呼叫 API
- **驗證重點**: 非 JavaScript 環境無法正常使用

##### TC-PW-JS-02: JavaScript 挑戰機制（可選）
- **測試步驟**:
  1. 測試頁面包含簡單計算挑戰
  2. JavaScript 自動解答並取得 Token
  3. 純 HTTP 請求無法取得正確 Token
- **驗證重點**: JavaScript 挑戰增加爬蟲難度

#### 4.6 Cookie 測試 (2 項)

##### TC-PW-COOKIE-01: SameSite Cookie 設定
- **測試步驟**:
  1. 檢查 API 回應的 Set-Cookie Header
  2. 驗證包含 `SameSite=Strict` 或 `SameSite=Lax`
  3. 嘗試跨域請求時驗證 Cookie 未被帶入
- **驗證重點**: SameSite 屬性防止 CSRF

##### TC-PW-COOKIE-02: Secure 與 HttpOnly 屬性
- **測試步驟**:
  1. 檢查 Cookie 包含 `Secure; HttpOnly`
  2. 使用 JavaScript 嘗試存取 Cookie
  3. 驗證無法存取（HttpOnly）
- **驗證重點**: Cookie 安全屬性設定正確

#### 4.7 使用者流程 (2 項)

##### TC-PW-FLOW-01: 完整表單提交流程
- **測試步驟**:
  1. 開啟測試頁面
  2. 填寫表單資料
  3. 點擊提交按鈕
  4. 自動取得 Token
  5. 發送 POST 請求到 `/api/protected`
  6. 顯示成功訊息
- **驗證重點**: 正常用戶流程順暢

##### TC-PW-FLOW-02: Token 失效後自動重試
- **測試步驟**:
  1. 提交表單
  2. 首次請求因 Token 過期失敗
  3. 自動重新取得 Token
  4. 重試請求成功
- **驗證重點**: 自動錯誤恢復機制

---

## 🔧 測試工具與方法

### 工具 1: cURL 命令列測試

適用於：API 層級的安全測試，模擬惡意請求

#### cURL 參數說明
- `-X POST`: HTTP POST 方法
- `-H`: 設定 HTTP Header
- `-d`: 設定請求 Body (JSON 格式)
- `-k`: 忽略 SSL 憑證驗證 (開發環境用)
- `-i`: 顯示 Response Header
- `-s`: 靜默模式 (不顯示進度)

#### API 參數
- `maxUsage`: Token 最大使用次數 (預設: 1)
- `expirationMinutes`: Token 過期時間，單位分鐘 (預設: 5)

### 工具 2: PowerShell 自動化腳本

#### 腳本 1: api-protected-security-test.ps1

**位置**: `tests/security/scripts/api-protected-security-test.ps1`

**功能**: 執行 10 項核心安全測試並產生報告

**執行方式**:
```powershell
cd tests/security/scripts
.\api-protected-security-test.ps1

# 或指定自訂 BaseUrl
.\api-protected-security-test.ps1 -BaseUrl "http://localhost:5073"
```

**腳本內容**: 參考 `tests/security/scripts/api-protected-security-test.ps1`

**測試項目**:
- TC-CSRF-01: 無 Token 的請求
- TC-CSRF-02: 偽造 Token 的請求
- TC-CSRF-03: 過期 Token 的請求
- TC-CSRF-04: 重複使用 Token
- TC-CSRF-05: CORS 政策檢查
- TC-LEAK-01: cURL 使用洩漏 Token
- TC-LEAK-02: Token 批次請求
- TC-BOT-01: 無 User-Agent 請求
- TC-BOT-02: 爬蟲 User-Agent
- TC-CURL-01: cURL 無 Token

#### 腳本 2: curl-security-test.ps1

**位置**: `tests/security/scripts/curl-security-test.ps1`

**功能**: 使用 PowerShell 執行 7 項 cURL 風格測試

**執行方式**:
```powershell
cd tests/security/scripts
.\curl-security-test.ps1
```

**腳本內容**: 參考 `tests/security/scripts/curl-security-test.ps1`

**測試項目**:
1. 缺少 Token Header - 應拒絕存取
2. 使用無效/偽造的 Token - 應拒絕存取
3. 使用有效 Token (首次使用) - 應允許存取
4. Token 重複使用 (第二次) - 應允許
5. Token 超過使用次數限制 (第三次) - 應拒絕
6. 使用過期的 Token - 應拒絕存取
7. 空白 Token Header - 應拒絕存取

#### 腳本 3: curl-security-test.sh

**位置**: `tests/security/scripts/curl-security-test.sh`

**功能**: Bash 版本的 cURL 安全測試 (Linux/macOS)

**執行方式**:
```bash
cd tests/security/scripts
chmod +x curl-security-test.sh
./curl-security-test.sh
```

**腳本內容**: 參考 `tests/security/scripts/curl-security-test.sh`

**測試項目**: 與 PowerShell 版本相同，共 7 項測試

#### 腳本 4: frontend-security-test.ps1

**位置**: `tests/security/scripts/frontend-security-test.ps1`

**功能**: CSRF 防護能力自動驗證

**執行方式**:
```powershell
cd tests/security/scripts
.\frontend-security-test.ps1
```

**腳本內容**: 參考 `tests/security/scripts/frontend-security-test.ps1`

**測試項目**:
1. 正常流程 - 驗證合法請求能通過
2. 缺少 Token - 驗證無 Token 請求被拒絕
3. 無效 Token - 驗證偽造 Token 被拒絕
4. Token 重複使用 - 驗證使用次數限制
5. Token 過期 - 驗證過期 Token 被拒絕
6. 並發請求 - 驗證並發請求處理

### 工具 3: Playwright 前端自動化測試

#### 安裝 Playwright
```bash
npm init playwright@latest
# 或加入現有專案
npm install -D @playwright/test
npx playwright install
```

#### Playwright 設定 (playwright.config.ts)

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/playwright',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/results.xml' }]
  ],
  use: {
    baseURL: 'https://localhost:5001',
    headless: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true,
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],

  webServer: {
    command: 'dotnet run --project Lab.CSRF2.WebAPI',
    url: 'https://localhost:5001',
    reuseExistingServer: !process.env.CI,
    ignoreHTTPSErrors: true,
  },
});
```

#### Playwright 測試範例 (tests/playwright/api-protected.spec.ts)

```typescript
import { test, expect } from '@playwright/test';

test.describe('API Protected Endpoint Tests', () => {

  test('TC-PW-TOKEN-01: 正常取得並使用 Token', async ({ page }) => {
    await page.goto('/test-page.html');

    const tokenResponse = await page.request.get('/api/token');
    const token = tokenResponse.headers()['x-csrf-token'];
    expect(token).toBeTruthy();

    const apiResponse = await page.request.post('/api/protected', {
      headers: {
        'X-CSRF-Token': token,
        'Content-Type': 'application/json',
      },
      data: { data: 'test' },
    });

    expect(apiResponse.status()).toBe(200);
  });

  test('TC-PW-TOKEN-03: Token 使用次數限制', async ({ page }) => {
    await page.goto('/test-page.html');

    const tokenResponse = await page.request.get('/api/token');
    const token = tokenResponse.headers()['x-csrf-token'];

    let successCount = 0;
    let firstFailureStatus = null;

    for (let i = 0; i < 10; i++) {
      const response = await page.request.post('/api/protected', {
        headers: { 'X-CSRF-Token': token },
        data: { data: `request-${i}` },
        failOnStatusCode: false,
      });

      if (response.status() === 200) {
        successCount++;
      } else if (!firstFailureStatus) {
        firstFailureStatus = response.status();
      }
    }

    expect(successCount).toBeLessThan(10);
    expect(firstFailureStatus).toBe(401);
  });

});
```

#### Playwright 執行指令

```bash
# 執行所有測試
npx playwright test

# 執行特定測試檔案
npx playwright test api-protected.spec.ts

# 執行特定瀏覽器
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit

# 顯示測試報告
npx playwright show-report

# Debug 模式
npx playwright test --debug

# UI 模式（互動式）
npx playwright test --ui

# 無頭模式執行
npx playwright test --headed=false
```

---

## 📊 預期測試結果總覽

### cURL 測試

| 測試項目 | 預期 HTTP 狀態碼 | 說明 |
|---------|----------------|------|
| 缺少 Token | 401 Unauthorized | 基本防護 |
| 無效 Token | 401 Unauthorized | 防偽造 |
| 有效 Token (首次) | 200 OK | 正常流程 |
| Token 第二次使用 | 200 OK | 次數限制未達 |
| Token 第三次使用 | 401 Unauthorized | 超過使用次數 |
| 過期 Token | 401 Unauthorized | 時效控制 |
| 空白 Token | 401 Unauthorized | 邊界條件 |

### Playwright 測試

所有測試案例應通過，驗證：
- Token 機制在瀏覽器環境正常運作
- CORS 政策正確阻擋跨域請求
- Header 自動帶入符合預期
- 多瀏覽器相容性
- JavaScript 環境驗證
- Cookie 安全屬性正確

---

## 📈 測試報告格式

### 單一測試案例格式
- **測試編號**: TC-XXXX-XX
- **測試名稱**: 案例描述
- **執行時間**: ISO 8601 格式
- **測試結果**: ✅ PASS / ❌ FAIL
- **實際回應**: HTTP Status Code + Response Body
- **備註**: 額外觀察或建議

### Playwright 自動產生報告

1. **HTML 報告**:
   - 位置: `playwright-report/index.html`
   - 包含詳細測試結果、截圖、影片

2. **JSON 報告**:
   - 位置: `test-results/results.json`
   - 可整合至 CI/CD 系統

3. **JUnit 報告**:
   - 位置: `test-results/results.xml`
   - 可整合至 Azure DevOps、Jenkins 等

---

## 🔒 建議防護機制優先順序

### 🔴 必須實作 (高優先級)
1. ✅ Token 驗證 (已實作)
2. ✅ Token 過期機制 (已實作)
3. ✅ Token 使用次數限制 (已實作)
4. ⚠️ CORS 政策設定
5. ⚠️ 速率限制 (Rate Limiting)

### 🟡 建議實作 (中優先級)
6. ⚠️ Referer / Origin Header 驗證
7. ⚠️ User-Agent 基本檢查
8. ⚠️ IP 地址綁定 (可選)

### 🟢 進階實作 (低優先級)
9. ❌ JavaScript 挑戰 / Captcha
10. ❌ Honeypot 陷阱欄位
11. ❌ 機器學習行為分析

---

## 🚨 風險評估

| 風險場景 | 嚴重程度 | 目前防護 | 建議改善 |
|---------|---------|---------|---------|
| CSRF 攻擊 | 🔴 高 | Token 驗證 | 新增 SameSite Cookie |
| Token 洩漏濫用 | 🟡 中 | 次數限制 | 新增 IP 綁定 + User-Agent 檢查 |
| 爬蟲批次請求 | 🟡 中 | Token 有效期 | 新增速率限制 |
| DDoS 攻擊 | 🔴 高 | 無 | 新增速率限制 + WAF |
| Referer/Origin 偽造 | 🟢 低 | CORS | CORS 已足夠 (瀏覽器強制) |

---

## 🎯 測試檢查清單總覽

### CSRF 防護測試: 7 項
- [ ] TC-CSRF-01 ~ TC-CSRF-07

### Token 洩漏防護測試: 4 項
- [ ] TC-LEAK-01 ~ TC-LEAK-04

### 爬蟲防護測試: 6 項
- [ ] TC-BOT-01 ~ TC-BOT-06

### 前端整合測試 (Playwright): 18 項
- [ ] TC-PW-TOKEN-01 ~ TC-PW-TOKEN-03 (Token 測試)
- [ ] TC-PW-CSRF-01 ~ TC-PW-CSRF-03 (CSRF 防護)
- [ ] TC-PW-HEADER-01 ~ TC-PW-HEADER-03 (Header 驗證)
- [ ] TC-PW-BROWSER-01 ~ TC-PW-BROWSER-03 (多瀏覽器)
- [ ] TC-PW-JS-01 ~ TC-PW-JS-02 (JavaScript 環境)
- [ ] TC-PW-COOKIE-01 ~ TC-PW-COOKIE-02 (Cookie 測試)
- [ ] TC-PW-FLOW-01 ~ TC-PW-FLOW-02 (使用者流程)

**總計**: 35 項測試案例

---

## 🔄 CI/CD 整合

### GitHub Actions 範例

```yaml
name: Security Tests
on: [push, pull_request]

jobs:
  curl-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Run API
        run: dotnet run --project Lab.CSRF2.WebAPI &
      - name: Run cURL Tests
        run: ./api-protected-security-test.sh

  playwright-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - name: Install dependencies
        run: npm ci
      - name: Install Playwright Browsers
        run: npx playwright install --with-deps
      - name: Run Playwright tests
        run: npx playwright test
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
          retention-days: 30
```

---

## ⚠️ 注意事項

1. **執行前提**: 確保 WebAPI 服務已啟動
   ```bash
   dotnet run --project Lab.CSRF2.WebAPI
   ```

2. **SSL 憑證**: 開發環境使用 `-k` 參數略過憑證檢查

3. **Token 格式**: Token 為 GUID 格式，儲存於 Server 端 Memory Cache

4. **環境變數**:
   - Linux/macOS: `$TOKEN`
   - Windows PowerShell: `$TOKEN`
   - Windows CMD: `%TOKEN%`

5. **換行符號**: Windows 的 `\r\n` 需使用 `tr -d '\r'` 清除

6. **測試隔離**: 不同測試案例應使用不同 Token，避免互相影響

---

## 📚 參考文件

### 安全標準
- [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [MDN - CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [RFC 6750 - Bearer Token Usage](https://datatracker.ietf.org/doc/html/rfc6750)

### 測試工具
- [Playwright 官方文件](https://playwright.dev/)
- [Playwright 最佳實踐](https://playwright.dev/docs/best-practices)
- [API Testing with Playwright](https://playwright.dev/docs/api-testing)
- [Network Interception](https://playwright.dev/docs/network)
- [cURL Documentation](https://curl.se/docs/)

---

## 📂 相關檔案

### 測試計畫文件
- `security-test-plan.md` - 本文件（整合版）
- `docs/api-protected-security-test-plan.md` - API 安全測試詳細計畫
- `docs/curl-security-test-plan.md` - cURL 測試計畫
- `docs/frontend-playwright-test-plan.md` - Playwright 測試計畫
- `docs/frontend-security-test-plan.md` - 前端安全測試計畫

### 測試腳本
- `tests/security/scripts/api-protected-security-test.ps1` - API 安全測試腳本 (PowerShell)
- `tests/security/scripts/curl-security-test.ps1` - cURL 測試腳本 (PowerShell)
- `tests/security/scripts/curl-security-test.sh` - cURL 測試腳本 (Bash)
- `tests/security/scripts/frontend-security-test.ps1` - 前端安全測試腳本 (PowerShell)
- `tests/security/playwright/` - Playwright 測試程式目錄

### 測試頁面
- `tests/security/fixtures/test.html` - 測試用主頁面
- `tests/security/fixtures/api-protected-test.html` - API 測試頁面

---

## 🎯 成功標準

所有測試項目需符合預期結果：
- ✅ 所有惡意請求（缺少/無效/過期 Token）均被拒絕
- ✅ 合法請求正常通過
- ✅ Token 使用次數限制正確執行
- ✅ CORS 政策正確阻擋跨域請求
- ✅ 無法透過模擬攻擊繞過防護
- ✅ 多瀏覽器相容性測試通過
- ✅ 自動化測試可重複執行且結果穩定

---

## 📝 版本紀錄

| 版本 | 日期 | 變更內容 | 作者 |
|------|------|---------|------|
| 2.0 | 2026-01-12 | 整合所有測試計畫為完整版 | Security Team |
| 1.0 | 2026-01-12 | 初版建立 | Security Team |
