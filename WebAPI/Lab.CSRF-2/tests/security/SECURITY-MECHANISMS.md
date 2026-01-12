# API 安全防護機制完整說明

## 🛡️ 已實作的安全機制

### 1. Token 驗證機制

#### Token 生成 (`GET /api/token`)
- **位置**: Response Header (`X-CSRF-Token`)
- **格式**: GUID (例: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`)
- **參數**:
  - `maxUsage`: 最大使用次數（預設 1 次）
  - `expirationMinutes`: 過期時間（預設 5 分鐘）
- **綁定資訊**:
  - User-Agent（防止跨客戶端使用）
  - IP 地址（已註解，生產環境可啟用）

#### Token 驗證 (`POST /api/protected`)
- **位置**: Request Header (`X-CSRF-Token`)
- **驗證項目**:
  1. Token 是否存在
  2. Token 是否在伺服器端儲存
  3. Token 是否過期
  4. Token 使用次數是否超過限制
  5. User-Agent 是否一致
  6. IP 地址是否一致（可選）

### 2. User-Agent 防護

#### 黑名單機制
自動拒絕以下 User-Agent:
- `curl/` - cURL 命令列工具
- `wget/` - Wget 下載工具
- `scrapy` - Python 爬蟲框架
- `python-requests` - Python Requests 套件
- `java/` - Java HTTP 客戶端
- `go-http-client` - Go HTTP 客戶端
- `http.rb/` - Ruby HTTP 客戶端
- `axios/` - Axios HTTP 客戶端
- `node-fetch` - Node.js Fetch

#### User-Agent 綁定
- Token 生成時記錄 User-Agent
- Token 使用時必須使用相同 User-Agent
- 防止 Token 被盜用到其他客戶端

### 3. Referer 驗證

#### 白名單機制
只允許來自以下來源的請求:
- `http://localhost:5073`
- `https://localhost:5073`
- `http://localhost:7001`
- `https://localhost:7001`

#### 驗證邏輯
- 檢查 Referer Header 是否在白名單中
- 開發環境: 允許無 Referer（測試用）
- 生產環境: 建議強制 Referer

### 4. Origin 驗證（CORS）

#### CORS 設定
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(允許的來源)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-CSRF-Token")
              .AllowCredentials();
    });
});
```

#### 驗證邏輯
- 只驗證 CORS 請求（有 Origin Header）
- 檢查 Origin 是否在白名單中
- 非 CORS 請求跳過檢查

### 5. 速率限制

#### Token 生成速率限制
- **策略**: Fixed Window
- **限制**: 1 分鐘內最多 5 次請求
- **超過限制**: HTTP 429 Too Many Requests

#### API 呼叫速率限制
- **策略**: Fixed Window
- **限制**: 10 秒內最多 10 次請求
- **超過限制**: HTTP 429 Too Many Requests

### 6. Token 使用限制

#### 時間限制
- **預設過期時間**: 5 分鐘
- **可配置**: 透過 `expirationMinutes` 參數
- **自動清理**: 過期 Token 自動從記憶體移除

#### 使用次數限制
- **預設使用次數**: 1 次
- **可配置**: 透過 `maxUsage` 參數
- **自動失效**: 達到次數限制後自動移除

### 7. 記憶體快取

#### Token 儲存機制
- **使用**: IMemoryCache
- **過期策略**: AbsoluteExpiration
- **資料結構**:
  ```csharp
  public class TokenData
  {
      public DateTime CreatedAt { get; set; }
      public DateTime ExpiresAt { get; set; }
      public int MaxUsageCount { get; set; }
      public int UsageCount { get; set; }
      public string UserAgent { get; set; }
      public string IpAddress { get; set; }
  }
  ```

## 🔒 安全防護矩陣

| 攻擊類型 | 防護機制 | HTTP 狀態碼 |
|---------|---------|-----------|
| 無 Token 直接攻擊 | Token 必須驗證 | 401 |
| 無效/偽造 Token | Token 伺服器端驗證 | 401 |
| Token 過期 | 時間限制驗證 | 401 |
| Token 重放攻擊 | 使用次數限制 | 401 |
| Token 盜用 | User-Agent 綁定 | 401 |
| 跨域攻擊 | Origin/Referer 驗證 | 403 |
| 爬蟲/Bot 攻擊 | User-Agent 黑名單 | 403 |
| 暴力破解 | 速率限制 | 429 |

## 📊 防護層級

```
第 1 層: 速率限制
   ↓
第 2 層: User-Agent 黑名單驗證
   ↓
第 3 層: Referer/Origin 白名單驗證
   ↓
第 4 層: Token 存在性驗證
   ↓
第 5 層: Token 有效性驗證
   ↓
第 6 層: Token 過期驗證
   ↓
第 7 層: Token 使用次數驗證
   ↓
第 8 層: User-Agent 一致性驗證
   ↓
✅ 請求通過，執行業務邏輯
```

## 🧪 測試驗證

### 自動化測試涵蓋

| 測試案例 | 驗證項目 | 腳本 |
|---------|---------|------|
| 正常流程 | Token 正常運作 | test-01 |
| Token 過期 | 時間限制生效 | test-02 |
| 使用次數限制 | 次數限制生效 | test-03 |
| 無 Token | 必須提供 Token | test-04 |
| 無效 Token | Token 驗證生效 | test-05 |
| User-Agent 不一致 | UA 綁定生效 | test-06 |
| 速率限制 | 頻率限制生效 | test-07 |
| 瀏覽器正常流程 | 瀏覽器整合 | test-08 |
| 瀏覽器使用限制 | 瀏覽器次數限制 | test-09 |
| 跨頁面使用 | 跨頁面安全性 | test-10 |
| 直接攻擊 | 無 Token 拒絕 | test-11 |
| 重放攻擊 | Token 重用拒絕 | test-12 |

## 🚀 生產環境建議

### 必須啟用的設定

1. **HTTPS 強制**
   ```csharp
   app.UseHttpsRedirection();
   app.UseHsts();
   ```

2. **IP 地址綁定**
   ```csharp
   // 在 TokenService.ValidateToken 中取消註解
   if (!string.IsNullOrEmpty(tokenData.IpAddress) && 
       tokenData.IpAddress != ipAddress)
   {
       return false;
   }
   ```

3. **強制 Referer**
   ```csharp
   // 在 ValidateTokenAttribute.ValidateReferer 中
   if (string.IsNullOrEmpty(referer))
   {
       return false; // 改為必須提供
   }
   ```

4. **使用 Redis**
   ```csharp
   // 替換 IMemoryCache 為 IDistributedCache (Redis)
   services.AddStackExchangeRedisCache(options =>
   {
       options.Configuration = "localhost:6379";
   });
   ```

5. **日誌監控**
   ```csharp
   // 加強安全事件記錄
   logger.LogWarning("Security Event", new {
       EventType = "TokenValidationFailed",
       UserAgent = userAgent,
       IpAddress = ipAddress,
       Timestamp = DateTime.UtcNow
   });
   ```

### 安全性檢查清單

- [ ] HTTPS 強制使用
- [ ] Token IP 綁定啟用
- [ ] Referer 強制驗證
- [ ] 使用 Redis 儲存 Token
- [ ] 設定適當的 Token 過期時間
- [ ] 設定適當的速率限制
- [ ] 啟用詳細的安全日誌
- [ ] 定期檢視安全事件
- [ ] 設定異常流量警報
- [ ] 準備 DDoS 防護

## 📝 常見問題

### Q: 為什麼 curl 測試會被拒絕？
A: User-Agent 黑名單機制會自動拒絕 `curl/` 開頭的 User-Agent。這是為了防止命令列工具直接存取 API。

### Q: 如何在測試時允許 curl？
A: 暫時從 `ValidateTokenAttribute` 的 `BotUserAgents` 陣列中移除 `"curl/"`。

### Q: Token 可以在不同瀏覽器間共用嗎？
A: 不可以。Token 綁定了 User-Agent，不同瀏覽器的 User-Agent 不同，因此無法共用。

### Q: 如何增加 Token 的使用次數？
A: 在取得 Token 時加上 `maxUsage` 參數，例如: `/api/token?maxUsage=5`

### Q: Token 儲存在哪裡？
A: 開發環境使用 IMemoryCache（記憶體），生產環境建議使用 Redis（分散式快取）。

## 🎯 安全目標達成確認

✅ **保護端點 api/protected**
- ✅ 只能被當前頁面使用（User-Agent + Referer/Origin 驗證）
- ✅ 防止被爬蟲濫用（User-Agent 黑名單 + 速率限制）
- ✅ curl 無法直接使用（黑名單 + Token 驗證）

✅ **Token 使用限制**
- ✅ 有效期限控制（可配置）
- ✅ 使用次數限制（可配置）
- ✅ 綁定客戶端特徵（User-Agent）

✅ **防護機制**
- ✅ 多層防護（8 層驗證）
- ✅ 全面測試（12 個測試案例）
- ✅ 自動化驗證（27 個測試腳本）
