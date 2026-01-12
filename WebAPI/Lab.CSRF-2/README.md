# Lab.CSRF-2 - Web API 防濫用機制實作

## 解決什麼問題?

本專案實作一套完整的 **Web API 防濫用機制**,保護可匿名存取的公開 API,防止以下安全威脅:

- ✅ **CSRF 攻擊** - 透過 Token 驗證機制防止跨站請求偽造
- ✅ **暴力攻擊** - 速率限制 (Rate Limiting) 防止短時間大量請求
- ✅ **重放攻擊** - Token 使用次數限制與過期機制
- ✅ **機器人攻擊** - User-Agent 黑名單過濾常見爬蟲與攻擊工具
- ✅ **Token 盜用** - Token 綁定 User-Agent,防止跨客戶端使用

### 核心防護機制

| 機制 | 說明 |
|------|------|
| Token 驗證 | 產生唯一 Token,每次請求必須攜帶 |
| 時效控制 | Token 10 分鐘後自動失效 |
| 使用次數限制 | 單一 Token 可配置使用次數 (預設 3 次) |
| User-Agent 綁定 | Token 綁定產生時的 User-Agent |
| User-Agent 黑名單 | 封鎖常見爬蟲與攻擊工具 |
| 速率限制 | 5 秒內最多 10 次請求 (同一 IP) |
| 強制 User-Agent | 產生 Token 時必須提供 User-Agent |

## 專案結構

```
Lab.CSRF-2/
├── Lab.CSRF2.WebAPI/              # ASP.NET Core Web API 專案
│   ├── Controllers/
│   │   ├── TokenController.cs     # Token 產生端點 (GET /api/token)
│   │   └── ProtectedController.cs # 受保護 API (POST /api/protected)
│   ├── Providers/
│   │   ├── ITokenProvider.cs      # Token 提供者介面
│   │   └── TokenProvider.cs       # Token 邏輯實作
│   ├── Filters/
│   │   └── ValidateTokenAttribute.cs # Token 驗證 Filter
│   ├── wwwroot/
│   │   ├── test.html              # 瀏覽器測試頁面
│   │   └── api-protected-test.html # API 受保護端點測試
│   └── Program.cs                 # 應用程式進入點
│
├── tests/                         # 測試資料夾
│   └── security/                  # 安全測試
│       ├── scripts/               # 測試腳本 (Bash + PowerShell)
│       │   ├── run-all-tests.ps1  # 執行所有測試
│       │   ├── test-01-normal-flow.ps1
│       │   ├── test-02-token-expiration.ps1
│       │   ├── test-03-usage-limit.ps1
│       │   ├── test-04-missing-token.ps1
│       │   ├── test-05-invalid-token.ps1
│       │   ├── test-05-4-missing-user-agent.ps1
│       │   ├── test-06-ua-mismatch.ps1
│       │   ├── test-07-rate-limiting.ps1
│       │   ├── test-11-direct-attack.ps1
│       │   ├── test-12-replay-attack.ps1
│       │   ├── test-08-browser-normal.spec.js
│       │   ├── test-09-browser-usage-limit.spec.js
│       │   └── test-10-cross-page.spec.js
│       ├── package.json           # Playwright 測試套件設定
│       ├── playwright.config.js   # Playwright 設定檔
│       ├── SECURITY-MECHANISMS.md # 安全機制詳細說明
│       └── security-test-plan.md  # 完整測試計畫
│
├── spec.md                        # 技術規格文件
├── tree.md                        # 專案結構說明
└── web-api-protect-plan.md        # 實作計畫文件
```

## 如何執行專案

### 前置需求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PowerShell (Windows) 或 Bash (Linux/macOS)
- (選用) [Node.js](https://nodejs.org/) - 執行 Playwright 測試時需要

### 啟動 Web API

```powershell
# 切換到專案目錄
cd Lab.CSRF2.WebAPI

# 還原相依套件
dotnet restore

# 執行專案
dotnet run
```

預設會在 `http://localhost:5000` 啟動服務。

### API 端點

| 端點 | 方法 | 說明 |
|------|------|------|
| `/api/token` | GET | 取得 Token (回傳於 Response Header `X-CSRF-Token`) |
| `/api/protected` | POST | 受保護的 API (需在 Request Header 攜帶 `X-CSRF-Token`) |

### 測試方式 1: 使用 cURL

```powershell
# 1. 取得 Token
$headers = curl -i http://localhost:5000/api/token -H "User-Agent: MyClient/1.0"
$token = ($headers | Select-String "x-csrf-token: (.+)").Matches.Groups[1].Value.Trim()

# 2. 使用 Token 呼叫受保護端點
curl http://localhost:5000/api/protected `
  -X POST `
  -H "X-CSRF-Token: $token" `
  -H "User-Agent: MyClient/1.0"
```

### 測試方式 2: 使用瀏覽器

1. 啟動 Web API 服務 (`dotnet run`)
2. 開啟瀏覽器,前往 `http://localhost:5000/test.html`
3. 點擊「取得 Token」按鈕
4. 點擊「呼叫受保護 API」按鈕

## 如何執行測試

### 測試環境設定

```powershell
# 切換到測試目錄
cd tests\security

# 安裝 Playwright (僅首次需要)
npm install
npx playwright install
```

### 執行測試

#### 方式 1: 執行所有測試 (推薦)

```powershell
# PowerShell
.\scripts\run-all-tests.ps1

# Bash (Linux/macOS)
./scripts/run-all-tests.sh
```

#### 方式 2: 執行個別測試

**cURL 測試 (PowerShell)**
```powershell
.\scripts\test-01-normal-flow.ps1           # 正常流程
.\scripts\test-02-token-expiration.ps1      # Token 過期
.\scripts\test-03-usage-limit.ps1           # 使用次數限制
.\scripts\test-04-missing-token.ps1         # 缺少 Token
.\scripts\test-05-invalid-token.ps1         # 無效 Token
.\scripts\test-05-4-missing-user-agent.ps1  # 缺少 User-Agent
.\scripts\test-06-ua-mismatch.ps1           # User-Agent 不一致
.\scripts\test-07-rate-limiting.ps1         # 速率限制
.\scripts\test-11-direct-attack.ps1         # 直接攻擊
.\scripts\test-12-replay-attack.ps1         # 重放攻擊
```

**Playwright 測試 (瀏覽器)**
```powershell
npm test                                    # 執行所有瀏覽器測試
npx playwright test test-08-browser-normal.spec.js      # 正常流程
npx playwright test test-09-browser-usage-limit.spec.js # 使用次數限制
npx playwright test test-10-cross-page.spec.js          # 跨頁面驗證
```

### 測試報告

```powershell
# 查看 Playwright 測試報告
npm run test:report
```

## 測試涵蓋範圍

本專案包含 **13 個測試案例**,涵蓋所有安全機制:

| 編號 | 測試名稱 | 工具 | 驗證項目 |
|------|----------|------|----------|
| 01 | 正常流程 | cURL | Token 產生與驗證成功 |
| 02 | Token 過期 | cURL | 10 分鐘後 Token 自動失效 |
| 03 | 使用次數限制 | cURL | Token 使用 3 次後失效 |
| 04 | 缺少 Token | cURL | 未攜帶 Token 回傳 401 |
| 05 | 無效 Token | cURL | 錯誤 Token 回傳 401 |
| 05-4 | 缺少 User-Agent | cURL | 產生 Token 時未提供 User-Agent 回傳 400 |
| 06 | User-Agent 不一致 | cURL | Token 綁定 User-Agent 驗證 |
| 07 | 速率限制 | cURL | 5 秒內超過 10 次請求被阻擋 |
| 08 | 瀏覽器正常流程 | Playwright | 瀏覽器環境 Token 驗證 |
| 09 | 瀏覽器使用次數 | Playwright | 瀏覽器 Token 使用次數限制 |
| 10 | 跨頁面驗證 | Playwright | 跨頁面 Token 共用 |
| 11 | 直接攻擊 | cURL | 不取得 Token 直接呼叫 API |
| 12 | 重放攻擊 | cURL | 使用已失效 Token 攻擊 |

## 安全機制詳細說明

詳見 [tests/security/SECURITY-MECHANISMS.md](tests/security/SECURITY-MECHANISMS.md)

## 授權

本專案為教學範例,僅供學習與研究使用。

## 作者

yowko's blog - https://blog.yowko.com
