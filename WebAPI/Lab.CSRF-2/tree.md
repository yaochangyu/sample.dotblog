# Lab.CSRF-2 專案結構

```
Lab.CSRF-2/
├── Lab.CSRF2.sln                                    # .NET Solution 方案檔
├── Lab.CSRF2.slnx                                   # Solution 擴充檔
├── README.md                                         # 專案說明文件
├── web-api-protect-plan.md                          # 實作計畫文件
├── spec.md                                           # 規格文件
├── test-api.ps1                                      # PowerShell 測試腳本
├── WebAPI防濫用機制.實作計畫.Progress.md            # 進度追蹤 (完成後刪除)
│
└── Lab.CSRF2.WebAPI/                                # Web API 專案
    ├── Lab.CSRF2.WebAPI.csproj                      # 專案檔
    ├── Program.cs                                    # 應用程式進入點
    ├── appsettings.json                             # 應用程式設定
    ├── appsettings.Development.json                 # 開發環境設定
    │
    ├── Controllers/                                  # API 控制器
    │   ├── TokenController.cs                       # Token 產生端點
    │   └── ProtectedController.cs                   # 受保護的 API 端點
    │
    ├── Services/                                     # 服務層
    │   ├── ITokenService.cs                         # Token 服務介面
    │   └── TokenService.cs                          # Token 服務實作
    │
    ├── Filters/                                      # 自訂 Filter
    │   └── ValidateTokenAttribute.cs                # Token 驗證 ActionFilter
    │
    ├── wwwroot/                                      # 靜態檔案
    │   └── test.html                                # HTML 測試頁面
    │
    ├── Properties/                                   # 專案屬性
    │   └── launchSettings.json                      # 啟動設定
    │
    ├── bin/                                          # 編譯輸出 (忽略)
    └── obj/                                          # 建置暫存 (忽略)
```

## 核心檔案說明

### 應用程式進入點
- **Program.cs** - 設定服務、CORS、Middleware 與路由

### Controllers (API 端點)
- **TokenController.cs** - 提供 GET /api/token 端點產生 Token
- **ProtectedController.cs** - 提供 POST /api/protected 端點示範受保護的 API

### Services (業務邏輯)
- **ITokenService.cs** - Token 服務介面定義
- **TokenService.cs** - Token 產生、驗證與儲存邏輯實作

### Filters (驗證機制)
- **ValidateTokenAttribute.cs** - ActionFilter 用於驗證 Request Header 中的 Token

### 測試資源
- **test.html** - 瀏覽器測試頁面，提供互動式測試介面
- **test-api.ps1** - PowerShell 自動化測試腳本

### 文件
- **README.md** - 完整專案說明與使用指南
- **web-api-protect-plan.md** - 詳細實作計畫與步驟
- **spec.md** - 技術規格文件

## 技術架構

```
┌─────────────────────────────────────────────┐
│            Client (Browser/cURL)            │
└─────────────────┬───────────────────────────┘
                  │
                  │ HTTP/HTTPS
                  │
┌─────────────────▼───────────────────────────┐
│         ASP.NET Core Pipeline               │
│  ┌─────────────────────────────────────┐   │
│  │  CORS Middleware                    │   │
│  └─────────────┬───────────────────────┘   │
│                │                             │
│  ┌─────────────▼───────────────────────┐   │
│  │  Controller Routing                 │   │
│  └─────────────┬───────────────────────┘   │
│                │                             │
│    ┌───────────▼────────────┐               │
│    │ TokenController        │               │
│    │ - GET /api/token       │               │
│    └───────────┬────────────┘               │
│                │                             │
│    ┌───────────▼──────────────────────┐     │
│    │ ProtectedController              │     │
│    │ - POST /api/protected            │     │
│    │ - [ValidateToken] Attribute      │     │
│    └───────────┬──────────────────────┘     │
└────────────────┼──────────────────────────┘
                 │
     ┌───────────▼───────────────┐
     │  ValidateTokenAttribute   │
     │  (ActionFilter)           │
     └───────────┬───────────────┘
                 │
     ┌───────────▼───────────────┐
     │  TokenService             │
     │  - GenerateToken()        │
     │  - ValidateToken()        │
     └───────────┬───────────────┘
                 │
     ┌───────────▼───────────────┐
     │  IMemoryCache             │
     │  (Token Storage)          │
     └───────────────────────────┘
```

## 資料流程

### Token 產生流程
1. Client → GET /api/token
2. TokenController → TokenService.GenerateToken()
3. TokenService → 產生 GUID → 儲存至 IMemoryCache
4. TokenService → 回傳 Token
5. TokenController → 在 Response Header 加入 X-CSRF-Token
6. Client ← 收到 Token

### Token 驗證流程
1. Client → POST /api/protected (帶 X-CSRF-Token Header)
2. ValidateTokenAttribute → 攔截請求
3. ValidateTokenAttribute → TokenService.ValidateToken()
4. TokenService → 從 IMemoryCache 取得 TokenData
5. TokenService → 驗證過期時間、使用次數
6. TokenService → 更新使用次數或刪除 Token
7. ValidateTokenAttribute → 驗證通過/失敗
8. ProtectedController → 處理請求 (驗證通過時)
9. Client ← 200 OK 或 401 Unauthorized

## 更新日期
2026-01-11
