# Device Fingerprint 裝置指紋驗證 - 實作計畫

## 問題陳述
在 ASP.NET Core 應用中實作裝置指紋驗證，確保每個帳號只能從已綁定的裝置登入。
新裝置首次登入時需通過 OTP 驗證，通過後才能綁定並正常使用。

## 技術選型
- **框架**：ASP.NET Core 10.0 Web API
- **資料庫**：PostgreSQL + EF Core（Npgsql）
- **認證**：JWT Bearer Token（含 fingerprint claim）
- **裝置識別**：FingerprintJS（前端） + SHA256 Hash（後端儲存）
- **OTP**：6 碼數字，存於 HybridCache（L1 記憶體 + L2 Redis，TTL 10 分鐘自動過期），以 log 模擬寄信
- **Cache**：`Microsoft.Extensions.Caching.Hybrid`（HybridCache）+ `StackExchange.Redis`（作為 L2 後端）

## 專案位置
`/mnt/d/lab/sample.dotblog/DeviceVerify/Lab.DeviceFingerprint.WebApi/`

## 架構流程

```
[前端 FingerprintJS]
       ↓ fingerprint (visitorId)
[POST /api/auth/login] → 驗證帳密
       ↓
  已知裝置？
  ├─ Yes → 產生 JWT（含 fingerprintHash claim）→ 回傳 token
  └─ No  → 產生 OTP → 記錄 log（模擬 Email）→ 回傳 { requireDeviceVerification: true }
                ↓
  [POST /api/auth/verify-device] → 驗證 OTP
                ↓
         綁定裝置 → 產生 JWT → 回傳 token

[後續 API 請求]
  → Header: X-Device-Fingerprint: {visitorId}
  → JWT claim: fingerprintHash
  → Middleware 比對 → 通過才放行
```

## 資料模型

```
User: Id, Username, PasswordHash, Email, CreatedAt
UserDevice: Id, UserId, FingerprintHash, DeviceName, UserAgent, IsVerified, CreatedAt, LastSeenAt
```

> OTP 使用 HybridCache 儲存，key = `device-otp:{userId}:{fingerprintHash}`，TTL = 10 分鐘，不需要 DB 資料表。
> HybridCache 採雙層架構：L1 = 記憶體（IMemoryCache），L2 = Redis（StackExchange.Redis）。

## 實作步驟

- [x] Step 1: 建立 ASP.NET Core 10 專案，加入 Npgsql.EF Core、Microsoft.Extensions.Caching.Hybrid、StackExchange.Redis、JWT、BCrypt 套件，建立 docker-compose.yml（PostgreSQL + Redis）
- [x] Step 2: 建立領域模型與 DbContext（含 Migration）
- [x] Step 3: 實作帳號認證服務（IAuthService），OTP 讀寫改用 `HybridCache`（`GetOrCreateAsync` / `SetAsync` / `RemoveAsync`）
- [x] Step 4: 實作裝置指紋驗證 Middleware
- [x] Step 5: 實作 API Controllers
- [x] Step 6: 建立前端測試頁面（整合 FingerprintJS）
- [x] Step 7: Build 並驗證功能
- [x] Step 8: 更新 tree.md
- [ ] Step 9: 建立 BDD 整合測試專案（SpecFlow），撰寫 Feature 涵蓋登入、新裝置 OTP 驗證、裝置指紋比對流程

## 注意事項
- 密碼使用 BCrypt 雜湊
- Fingerprint 在後端以 SHA256 雜湊後儲存，不存明文
- OTP 實際以 Console.WriteLine 模擬寄信（Demo 用途）
- JWT 過期時間：1 小時；DeviceToken 不過期（可手動撤銷）
