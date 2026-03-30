# Lab.DeviceFingerprint

裝置指紋驗證範例專案。透過 [FingerprintJS](https://github.com/fingerprintjs/fingerprintjs) 識別裝置，搭配 OTP 驗證流程，確保帳號只能從已綁定的裝置登入。

## 技術選型

| 類別 | 技術 |
|---|---|
| 框架 | ASP.NET Core 10.0 Web API |
| 資料庫 | PostgreSQL 16 + EF Core（Npgsql） |
| 快取 | HybridCache（L1 記憶體 + L2 Redis） |
| 認證 | JWT Bearer Token |
| 裝置識別 | FingerprintJS v4（前端）+ SHA256（後端儲存） |
| OTP | 6 碼亂數，存於 HybridCache（TTL 10 分鐘） |
| 密碼雜湊 | BCrypt.Net-Next |
| 測試 | Reqnroll（BDD）+ xUnit + WebApplicationFactory |

## 專案結構

```
Lab.DeviceFingerprint/
├── Lab.DeviceFingerprint.sln
├── Taskfile.yml                          # 開發自動化指令
├── .gitignore
│
├── Lab.DeviceFingerprint.WebApi/         # ASP.NET Core 10 Web API
│   ├── Application/
│   │   ├── DTOs/AuthDtos.cs              # Request / Response DTO
│   │   └── Services/
│   │       ├── IAuthService.cs
│   │       ├── AuthService.cs            # 登入、OTP、JWT 邏輯
│   │       └── IOtpGenerator.cs          # OTP 產生介面（可替換）
│   ├── Controllers/
│   │   ├── AuthController.cs             # POST /api/auth/login, /verify-device
│   │   ├── MeController.cs               # GET /api/me（受保護）
│   │   └── DevController.cs              # POST /api/dev/seed（Development only）
│   ├── Domain/Entities/
│   │   ├── User.cs
│   │   └── UserDevice.cs
│   ├── Infrastructure/
│   │   ├── Middleware/DeviceFingerprintMiddleware.cs
│   │   └── Persistence/AppDbContext.cs + Migrations/
│   ├── wwwroot/index.html                # 前端測試頁面（FingerprintJS）
│   ├── appsettings.json
│   └── docker-compose.yml               # PostgreSQL 16 + Redis 7
│
└── Lab.DeviceFingerprint.IntegrationTests/  # BDD 整合測試
    ├── Features/DeviceFingerprint.feature
    ├── Steps/AuthStepDefinitions.cs
    └── Support/TestContext.cs            # WebApplicationFactory + InMemory DB
```

## 驗證流程

```
[前端 FingerprintJS]
       ↓ fingerprint (visitorId)
[POST /api/auth/login] → 驗證帳密
       ↓
  已知裝置？
  ├─ Yes → 產生 JWT（含 fingerprintHash claim）→ 回傳 token
  └─ No  → 產生 OTP → 寫入 HybridCache（TTL 10m）→ 印出 log（模擬 Email）
                ↓
  [POST /api/auth/verify-device] → 驗證 OTP
                ↓
         綁定裝置 → 產生 JWT → 回傳 token

[後續 API 請求]
  Header: X-Device-Fingerprint: {visitorId}
  JWT claim: fingerprintHash
  → DeviceFingerprintMiddleware 比對 SHA256 → 不符回傳 403
```

## 快速開始

> 需要：Docker、.NET 10 SDK、[Task](https://taskfile.dev)

```bash
# 1. 啟動 PostgreSQL + Redis、建立 Schema、建立測試帳號，再啟動 API
task dev:start

# 2. 開啟瀏覽器
#    http://localhost:5120
#    帳號：admin / password123
#    OTP 查看 API console log
```

## Task 指令清單

```bash
task --list
```

| 指令 | 說明 |
|---|---|
| `task dev:start` | 一鍵啟動（infra + migrate + seed + API） |
| `task dev:setup` | 啟動 infra + migrate + seed（不啟動 API） |
| `task api:run` | 啟動 WebAPI（http://localhost:5120） |
| `task api:watch` | 啟動 WebAPI（hot reload） |
| `task test` | 執行所有 BDD 整合測試 |
| `task test:filter NAME=密碼錯誤` | 執行指定 Scenario |
| `task build` | Build 所有專案 |
| `task infra:up` | 啟動 Docker 容器 |
| `task infra:down` | 停止 Docker 容器 |
| `task db:migrate` | 執行 EF Core Migration |
| `task db:seed` | 建立測試帳號（admin / password123） |

## API 端點

| 方法 | 路徑 | 說明 | 需認證 |
|---|---|---|---|
| POST | `/api/auth/login` | 帳密 + 指紋登入 | ✗ |
| POST | `/api/auth/verify-device` | OTP 裝置驗證 | ✗ |
| GET | `/api/me` | 取得個人資料與裝置列表 | ✓ |
| POST | `/api/dev/seed` | 建立測試帳號（Development only） | ✗ |

## BDD 整合測試

測試使用 **Reqnroll + xUnit + WebApplicationFactory**，不需要啟動真實的 PostgreSQL 或 Redis。

```bash
task test
```

涵蓋 7 個 Scenario：

- ✅ 從已知驗證裝置登入，直接回傳 JWT
- ✅ 從新裝置登入，需要 OTP 驗證
- ✅ 以正確 OTP 驗證新裝置，綁定裝置並回傳 JWT
- ✅ 提交錯誤 OTP，回傳 401
- ✅ 使用正確 token 與符合指紋存取受保護端點，回傳 200
- ✅ 使用不符的指紋存取受保護端點，回傳 403
- ✅ 密碼錯誤，回傳 401

## HybridCache OTP 設計

```csharp
// 寫入 OTP（L1 記憶體 + L2 Redis，TTL 10 分鐘）
await cache.SetAsync(key, otp,
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) });

// 讀取 OTP
var stored = await cache.GetOrCreateAsync<string?>(key, _ => ValueTask.FromResult<string?>(null));

// 驗證成功後立即失效
await cache.RemoveAsync(key);
```

## 注意事項

- Fingerprint 後端以 SHA256 雜湊後儲存，不存明文
- JWT 過期時間：1 小時
- `Jwt:Key` 請在正式環境替換為安全的金鑰
- `/api/dev/seed` 僅在 `ASPNETCORE_ENVIRONMENT=Development` 時可用
