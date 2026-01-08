# Lab.CSRF.WebApi - CSRF 保護實作

這是一個展示如何在 ASP.NET Core Web API 中實作 CSRF（跨站請求偽造）保護機制的專案。

## 功能特色

- ✅ Anti-CSRF Token 機制
- ✅ 自動驗證 POST/PUT/DELETE 請求
- ✅ 基於記憶體快取的 Token 管理
- ✅ CORS 跨域資源共享設定
- ✅ 完整的測試頁面
- ✅ Token 過期管理（30 分鐘）

## 專案結構

```
Lab.CSRF.WebApi/
├── Controllers/
│   └── CsrfController.cs          # CSRF 測試端點
├── Filters/
│   └── ValidateCsrfTokenAttribute.cs  # CSRF Token 驗證 Filter
├── Services/
│   ├── ICsrfTokenService.cs       # CSRF Token 服務介面
│   └── CsrfTokenService.cs        # CSRF Token 服務實作
├── wwwroot/
│   └── index.html                 # 測試頁面
└── Program.cs                     # 應用程式進入點
```

## CSRF 保護機制

### 運作原理

1. **Token 產生**：客戶端向 API 請求一個唯一的 CSRF Token
2. **Token 儲存**：Token 儲存在伺服器端的記憶體快取中（30 分鐘有效期）
3. **Token 驗證**：所有寫入操作（POST/PUT/DELETE）必須在 Header 中帶上正確的 Token
4. **自動攔截**：使用 `ValidateCsrfTokenAttribute` 自動驗證請求

### 防護層次

#### 1. Anti-CSRF Token
- 每個頁面都必須先取得唯一的 Token
- Token 必須在 `X-CSRF-Token` Header 中傳送
- Token 有時效性（30 分鐘）
- 驗證失敗回傳 403 Forbidden

#### 2. CORS 限制
- 只允許特定網域的請求（`http://localhost:5173`, `http://localhost:3000`, `http://127.0.0.1:5500`）
- 限制可呼叫的 HTTP 方法
- 控制允許的 Header

#### 3. 自訂 Header 檢查
- 利用瀏覽器同源政策
- 跨站請求無法設定自訂 Header（`X-CSRF-Token`）
- 只有同源或 CORS 允許的網站才能發送

## API 端點

### 取得 CSRF Token
```
GET /api/csrf/token
```

回應：
```json
{
  "token": "base64-encoded-token",
  "expiresIn": 1800
}
```

### 測試受保護的端點（需要 Token）

#### POST 測試
```
POST /api/csrf/test
Headers: X-CSRF-Token: {token}
Body: { "message": "test message" }
```

#### PUT 測試
```
PUT /api/csrf/update/{id}
Headers: X-CSRF-Token: {token}
Body: { "message": "updated message" }
```

#### DELETE 測試
```
DELETE /api/csrf/delete/{id}
Headers: X-CSRF-Token: {token}
```

### 公開端點（不需要 Token）
```
GET /api/csrf/public
```

## 快速開始

### 1. 執行專案

```bash
cd Lab.CSRF.WebApi
dotnet run
```

預設會在 `https://localhost:7001` 啟動。

### 2. 開啟測試頁面

在瀏覽器中開啟：
```
https://localhost:7001/index.html
```

### 3. 測試流程

1. **取得 Token**：點擊「取得 CSRF Token」按鈕
2. **測試受保護端點**：輸入訊息，點擊「送出 POST 請求」
3. **測試缺少 Token**：點擊「測試不帶 Token」，應該會被拒絕（403）
4. **測試其他方法**：測試 PUT 和 DELETE 端點
5. **測試公開端點**：呼叫不需要 Token 的端點

## 使用範例

### JavaScript 前端整合

```javascript
// 1. 取得 Token
const response = await fetch('https://localhost:7001/api/csrf/token');
const { token } = await response.json();

// 2. 發送受保護的請求
await fetch('https://localhost:7001/api/csrf/test', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-CSRF-Token': token  // 必須帶上 Token
    },
    body: JSON.stringify({ message: 'Hello' })
});
```

### C# 客戶端整合

```csharp
using var client = new HttpClient();

// 1. 取得 Token
var tokenResponse = await client.GetFromJsonAsync<TokenResponse>(
    "https://localhost:7001/api/csrf/token");

// 2. 發送受保護的請求
var request = new HttpRequestMessage(HttpMethod.Post,
    "https://localhost:7001/api/csrf/test");
request.Headers.Add("X-CSRF-Token", tokenResponse.Token);
request.Content = JsonContent.Create(new { message = "Hello" });

var response = await client.SendAsync(request);
```

## 如何套用到現有專案

### 1. 安裝相依套件
```bash
# 記憶體快取已內建於 ASP.NET Core，無需額外安裝
```

### 2. 註冊服務（Program.cs）
```csharp
using Lab.CSRF.WebApi.Services;

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICsrfTokenService, CsrfTokenService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://your-frontend.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-CSRF-Token");
    });
});
```

### 3. 套用到 Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class YourController : ControllerBase
{
    [HttpPost]
    [ValidateCsrfToken]  // 加上這個屬性
    public IActionResult CreateData([FromBody] YourData data)
    {
        // 你的邏輯
    }
}
```

### 4. 提供 Token 端點
```csharp
[HttpGet("token")]
public IActionResult GetToken([FromServices] ICsrfTokenService csrfService)
{
    var token = csrfService.GenerateToken();
    return Ok(new { token });
}
```

## 安全性考量

### ✅ 已實作
- Token 有時效性（30 分鐘）
- Token 使用密碼學安全的亂數產生器
- 只驗證寫入操作（POST/PUT/DELETE）
- CORS 限制來源網域
- 自訂 Header 防護

### ⚠️ 生產環境建議
- 使用 HTTPS（強制）
- 將 Token 與使用者 Session 綁定
- 考慮使用 Redis 等分散式快取（多伺服器環境）
- 設定更嚴格的 CORS 規則
- 加入請求頻率限制（Rate Limiting）
- 記錄可疑的驗證失敗
- 定期輪換 Token

### 🚫 已防護的攻擊
- **CSRF 攻擊**：惡意網站無法取得有效的 Token
- **重放攻擊**：Token 有時效性，過期後無法使用
- **跨域攻擊**：CORS 限制只有指定網域可呼叫

### ❌ 不防護的攻擊
- **XSS 攻擊**：如果網站存在 XSS 漏洞，攻擊者可以讀取 Token
- **中間人攻擊**：需要搭配 HTTPS 防護
- **暴力破解**：需要搭配 Rate Limiting 防護

## 測試案例

專案包含以下測試案例：

1. ✅ 正常流程：取得 Token → 帶 Token 呼叫 API
2. ❌ 缺少 Token：直接呼叫受保護的 API（應該失敗）
3. ❌ 錯誤 Token：使用不存在的 Token（應該失敗）
4. ❌ 過期 Token：使用 30 分鐘前的 Token（應該失敗）
5. ✅ 公開端點：呼叫不需要 Token 的端點

## 常見問題

### Q: 為什麼 GET 請求不需要驗證？
A: GET 請求應該是冪等且安全的，不應該修改伺服器狀態。CSRF 攻擊主要針對會改變狀態的操作（POST/PUT/DELETE）。

### Q: Token 儲存在哪裡？
A: 目前儲存在伺服器端的記憶體快取中。在多伺服器環境中，建議使用 Redis 等分散式快取。

### Q: 客戶端應該如何儲存 Token？
A: 可以儲存在 JavaScript 變數、sessionStorage 或 localStorage 中。注意：如果儲存在 Storage 中，要防範 XSS 攻擊。

### Q: 可以用 Cookie 傳送 Token 嗎？
A: 不建議。使用 Cookie 傳送 Token 會失去 CSRF 保護的效果（因為瀏覽器會自動帶上 Cookie）。應該使用自訂 Header。

### Q: 這個機制可以防止所有攻擊嗎？
A: 不行。這只防護 CSRF 攻擊。你還需要：
- 防止 XSS（輸入驗證、輸出編碼）
- 防止 SQL Injection（使用參數化查詢）
- 防止暴力破解（Rate Limiting）
- 使用 HTTPS（防止中間人攻擊）

## 授權

MIT License

## 相關資源

- [OWASP CSRF 防護指南](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [ASP.NET Core 安全性](https://learn.microsoft.com/zh-tw/aspnet/core/security/)
- [CORS 設定](https://learn.microsoft.com/zh-tw/aspnet/core/security/cors)
