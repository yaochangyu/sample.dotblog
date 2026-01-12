# WebAPI 防濫用機制 - Token 驗證實作

## 📋 專案說明

本專案實作基於 Token 的 Web API 防濫用機制，使用 ASP.NET Core Web API (.NET 10) 建立。透過自訂 Token 產生、儲存與驗證機制，防止 API 被濫用或遭受 CSRF 攻擊。

## 🎯 核心功能

### 1. Token 管理
- ✅ 動態產生 GUID 格式 Token
- ✅ 可設定 Token 過期時間
- ✅ 可設定 Token 使用次數限制
- ✅ Server 端使用 IMemoryCache 儲存 Token

### 2. API 端點
- **GET /api/token** - 取得新的 Token
  - 參數：`maxUsage` (最大使用次數，預設 1)
  - 參數：`expirationMinutes` (過期時間，預設 5 分鐘)
  - 回應：在 Response Header 的 `X-CSRF-Token` 中回傳 Token

- **POST /api/protected** - 受保護的 API 端點
  - 需在 Request Header 帶入 `X-CSRF-Token`
  - 驗證 Token 有效性、過期時間與使用次數

### 3. 安全防護
- ✅ Token 過期自動失效
- ✅ Token 使用次數達上限後自動失效
- ✅ 無效或偽造 Token 拒絕存取
- ✅ 缺少 Token 拒絕存取
- ✅ CORS 支援，允許瀏覽器跨域呼叫

## 🏗️ 專案架構

```
Lab.CSRF-2/
├── tests/                             # 測試資料夾 ⭐ NEW
│   └── security/                      # 安全測試
│       ├── scripts/                   # 測試腳本
│       │   ├── api-protected-security-test.ps1
│       │   ├── curl-security-test.ps1
│       │   ├── curl-security-test.sh
│       │   └── frontend-security-test.ps1
│       ├── playwright/                # Playwright 測試
│       └── fixtures/                  # 測試頁面
│           ├── test.html
│           └── api-protected-test.html
├── docs/                              # 詳細文件 ⭐ NEW
│   ├── api-protected-security-test-plan.md
│   ├── curl-security-test-plan.md
│   ├── frontend-playwright-test-plan.md
│   └── frontend-security-test-plan.md
├── Lab.CSRF2.WebAPI/                  # WebAPI 專案
│   ├── Controllers/
│   │   ├── TokenController.cs
│   │   └── ProtectedController.cs
│   ├── Services/
│   │   ├── ITokenService.cs
│   │   └── TokenService.cs
│   ├── Filters/
│   │   └── ValidateTokenAttribute.cs
│   ├── wwwroot/
│   │   └── test.html
│   └── Program.cs
├── security-test-plan.md              # 完整安全測試計畫 ⭐ NEW
└── README.md                          # 本文件
```

## 🚀 快速開始

### 1. 編譯與執行

```powershell
cd Lab.CSRF2.WebAPI
dotnet build
dotnet run
```

預設執行於：
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5000`

### 2. 測試方式

#### 方式一：完整安全測試 (推薦) ⭐ NEW

```powershell
# API 安全測試 (10 項測試)
cd tests/security/scripts
.\api-protected-security-test.ps1

# cURL 測試 (7 項測試)
.\curl-security-test.ps1

# 前端安全測試 (6 項測試)
.\frontend-security-test.ps1
```

**Bash 版本 (Linux/macOS)**:
```bash
cd tests/security/scripts
chmod +x curl-security-test.sh
./curl-security-test.sh
```

#### 方式二：Playwright 前端自動化測試 ⭐ NEW

```bash
# 首次執行需安裝
npm install
npx playwright install

# 執行所有測試
npx playwright test

# 檢視報告
npx playwright show-report
```

#### 方式三：使用瀏覽器測試頁面
開啟瀏覽器訪問：
```
https://localhost:7001/test.html
```

或使用測試頁面：
```
開啟 tests/security/fixtures/test.html
```

#### 方式四：手動使用 cURL 或 PowerShell

**取得 Token:**
```powershell
$response = Invoke-WebRequest -Uri "https://localhost:7001/api/token?maxUsage=2&expirationMinutes=5" -SkipCertificateCheck
$token = $response.Headers['X-CSRF-Token']
```

**呼叫受保護的 API:**
```powershell
$headers = @{
    "X-CSRF-Token" = $token
    "Content-Type" = "application/json"
}
$body = @{ data = "測試資料" } | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:7001/api/protected" -Method Post -Headers $headers -Body $body -SkipCertificateCheck
```

## 🧪 安全性測試案例

### 完整測試涵蓋範圍 (35 項測試) ⭐ NEW

#### CSRF 防護測試 (7 項)
- 無 Token 請求驗證
- 偽造 Token 驗證
- 過期 Token 驗證
- Token 使用次數限制
- CORS 跨域請求驗證
- Referer/Origin Header 驗證

#### Token 洩漏防護測試 (4 項)
- cURL 使用洩漏 Token
- Token 批次請求攻擊
- 不同 IP 使用 Token
- 不同 User-Agent 使用 Token

#### 爬蟲防護測試 (6 項)
- 無 User-Agent 請求
- 可疑 User-Agent 黑名單
- 高頻率請求 (速率限制)
- Token 生成頻率限制
- JavaScript 挑戰機制
- Honeypot 陷阱欄位

#### 前端整合測試 - Playwright (18 項)
- Token 機制測試 (3 項)
- CSRF 防護測試 (3 項)
- Header 驗證測試 (3 項)
- 多瀏覽器相容性 (3 項)
- JavaScript 環境驗證 (2 項)
- Cookie 安全測試 (2 項)
- 使用者流程測試 (2 項)

### 基本測試 (舊版相容)

執行 `test-api.ps1` 會自動測試以下情境：

1. ✅ **取得 Token** - 驗證 Token 產生機制
2. ✅ **有效 Token 第一次使用** - 驗證正常流程
3. ✅ **有效 Token 第二次使用** - 驗證使用次數計數
4. ❌ **Token 使用次數超過限制** - 應回傳 401 Unauthorized
5. ❌ **使用無效 Token** - 應回傳 401 Unauthorized
6. ❌ **缺少 Token Header** - 應回傳 401 Unauthorized

### 詳細測試文件

- **[完整安全測試計畫](./security-test-plan.md)** - 整合所有測試的完整文件
- **[API 安全測試計畫](./docs/api-protected-security-test-plan.md)**
- **[cURL 測試計畫](./docs/curl-security-test-plan.md)**
- **[Playwright 測試計畫](./docs/frontend-playwright-test-plan.md)**
- **[前端安全測試計畫](./docs/frontend-security-test-plan.md)**

## 🔧 技術選型

| 項目 | 技術 |
|------|------|
| 框架 | ASP.NET Core Web API (.NET 10) |
| Token 儲存 | IMemoryCache |
| Token 格式 | GUID |
| 驗證方式 | Custom ActionFilter |
| CORS | 允許所有來源 (開發環境) |

## 📝 使用範例

### JavaScript 呼叫範例

```javascript
// 1. 取得 Token
const tokenResponse = await fetch('https://localhost:7001/api/token?maxUsage=1&expirationMinutes=5');
const token = tokenResponse.headers.get('X-CSRF-Token');

// 2. 呼叫受保護的 API
const response = await fetch('https://localhost:7001/api/protected', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-CSRF-Token': token
    },
    body: JSON.stringify({ data: '測試資料' })
});

const result = await response.json();
console.log(result);
```

## ⚠️ 注意事項

1. **開發環境設定**：目前 CORS 設定為允許所有來源，生產環境請限制特定來源
2. **HTTPS 憑證**：開發環境使用自簽憑證，測試時需加入 `-SkipCertificateCheck` 參數
3. **Token 儲存**：使用 IMemoryCache，應用程式重啟後 Token 會消失
4. **擴充性**：可改用 Redis 或資料庫儲存 Token 以支援分散式環境

## 📊 驗證流程

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant TokenService
    participant Cache

    Client->>API: GET /api/token
    API->>TokenService: GenerateToken()
    TokenService->>Cache: 儲存 Token + 元資料
    TokenService-->>API: 回傳 Token
    API-->>Client: X-CSRF-Token Header

    Client->>API: POST /api/protected (帶 Token)
    API->>TokenService: ValidateToken()
    TokenService->>Cache: 檢查 Token 存在性
    TokenService->>TokenService: 驗證過期時間
    TokenService->>TokenService: 驗證使用次數
    TokenService->>Cache: 更新使用次數
    TokenService-->>API: 驗證結果
    API-->>Client: 200 OK / 401 Unauthorized
```

## 🤝 貢獻

歡迎提出 Issue 或 Pull Request！

## 📄 授權

MIT License
