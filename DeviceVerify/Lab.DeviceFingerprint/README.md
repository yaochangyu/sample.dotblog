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

## 裝置指紋取得方式

### 前端：FingerprintJS

```javascript
const fp = await FingerprintJS.load();
const result = await fp.get();
const visitorId = result.visitorId; // 傳送至後端
```

FingerprintJS 在瀏覽器端收集多種訊號，組合雜湊產生 `visitorId`：

| 類別 | 訊號來源 |
|---|---|
| 硬體 | CPU 核心數、螢幕解析度、GPU（WebGL）、記憶體大小 |
| 瀏覽器 | User-Agent、語言設定、時區、Cookie 啟用狀態 |
| 字型 | 系統安裝的字型清單 |
| Canvas | 繪圖結果差異（不同硬體／驅動渲染略有不同） |
| Audio | AudioContext 輸出特徵 |
| 插件 | 瀏覽器已安裝的插件列表 |

### 後端：SHA256 雜湊儲存

前端傳來的 `visitorId`（明文）在後端**立即 SHA256 雜湊**後才存入資料庫，DB 只存 hash，不存原始值。

```csharp
// AuthService.cs
private static string HashFingerprint(string fingerprint)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}
```

### Middleware 比對流程

```
請求 Header: X-Device-Fingerprint: {visitorId}
                    ↓ SHA256
JWT claim:   fingerprintHash: 3a7f2b...  ←→  DeviceFingerprintMiddleware 比對
                    ↓ 相符 → 放行
                    ↓ 不符 → 403 Forbidden
```

> **注意**：FingerprintJS 免費版穩定性約 40–60%，同裝置不同瀏覽器或無痕模式可能產生不同 ID。
> 若需高精準度可改用 [FingerprintJS Pro](https://fingerprint.com)（號稱 99.5%）。

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
