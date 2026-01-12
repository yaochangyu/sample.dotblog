# ASP.NET Core Web API 防濫用機制實戰：Token、速率限制與多層防護

## 前言

最近在開發公開 API 時，遇到一個常見的問題：**如何保護可匿名存取的 API，防止被惡意濫用、爬蟲掃描或重放攻擊？**

雖然網路上有許多關於 CSRF 防護的文章，但大多聚焦在「瀏覽器表單提交」的場景。而對於「公開 API」的防護，往往只提到「加上 Token 就好」，卻沒有說明：
- Token 該如何設計？
- 如何防止 Token 被盜用？
- 如何避免被 curl 或爬蟲工具直接呼叫？
- 速率限制該如何實作？

本文將分享一套完整的多層防護機制，並透過 **19 個自動化測試案例**驗證防護效果。

---

## 防護機制有哪些？

根據實際需求，我設計了以下 8 層防護機制：

### 第 1 層：速率限制（Rate Limiting）
防止暴力破解與高頻率攻擊
- **Token 生成速率限制**：1 分鐘內最多 5 個 Token
- **API 呼叫速率限制**：10 秒內最多 10 次請求
- 超過限制回傳 `HTTP 429 Too Many Requests`

### 第 2 層：User-Agent 黑名單驗證
自動拒絕常見爬蟲工具
- 黑名單：`curl/`, `wget/`, `scrapy`, `python-requests`, `java/`, `go-http-client`, `axios/`, `node-fetch` 等
- 回傳 `HTTP 403 Forbidden`

### 第 3 層：Referer/Origin 白名單驗證
限制 API 只能從指定來源呼叫
- 白名單：`http://localhost:5073`, `https://localhost:5073` 等
- 開發環境：允許無 Referer（方便測試）
- 生產環境：建議強制 Referer

### 第 4 層：Token 存在性驗證
確保 Request Header 包含 `X-CSRF-Token`
- 回傳 `HTTP 401 Unauthorized`

### 第 5 層：Token 有效性驗證
檢查 Token 是否在 Server 端儲存
- 回傳 `HTTP 401 Unauthorized`

### 第 6 層：Token 過期驗證
預設 5 分鐘過期（可配置）
- 回傳 `HTTP 401 Unauthorized`

### 第 7 層：Token 使用次數驗證
預設單次使用（可配置）
- 防止重放攻擊
- 回傳 `HTTP 401 Unauthorized`

### 第 8 層：User-Agent 一致性驗證
Token 綁定取得時的 User-Agent
- 防止 Token 被盜用到其他客戶端
- 回傳 `HTTP 401 Unauthorized`

---

## 怎麼做防護？

### Server Side 配置

#### 1. Program.cs - 註冊服務與 CORS 設定

```csharp
var builder = WebApplication.CreateBuilder(args);

// 註冊記憶體快取與 Token 服務
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();

// ✅ 速率限制設定
builder.Services.AddRateLimiter(options =>
{
    // API 端點速率限制: 10 秒內最多 10 次請求
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueLimit = 0;
    });

    // Token 生成速率限制: 1 分鐘內最多 5 個 Token
    options.AddFixedWindowLimiter("token", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 5;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = 429; // Too Many Requests
});

// ✅ CORS 白名單設定（❌ 不使用 AllowAnyOrigin）
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5073",
                "https://localhost:5073",
                "http://localhost:7001",
                "https://localhost:7001"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-CSRF-Token") // 允許前端讀取 Token
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRateLimiter(); // ✅ 啟用速率限制中介層
app.UseCors();
app.MapControllers();
app.Run();
```

#### 2. TokenController.cs - Token 生成端點

```csharp
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("token")] // ✅ 套用 Token 速率限制
public class TokenController : ControllerBase
{
    private readonly ITokenProvider _tokenProvider;

    public TokenController(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    [HttpGet]
    public IActionResult GetToken(
        [FromQuery] int maxUsage = 1,
        [FromQuery] int expirationMinutes = 5)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 生成 Token 並綁定 User-Agent 與 IP
        var token = _tokenProvider.GenerateToken(maxUsage, expirationMinutes, userAgent, ipAddress);

        // ✅ Token 放在 Response Header
        Response.Headers["X-CSRF-Token"] = token;

        return Ok(new { message = "Token generated successfully", token });
    }
}
```

#### 3. ProtectedController.cs - 受保護端點

```csharp
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")] // ✅ 套用 API 速率限制
public class ProtectedController : ControllerBase
{
    [HttpPost]
    [ValidateToken] // ✅ 套用 Token 驗證 Attribute
    public IActionResult PostData([FromBody] ProtectedRequest request)
    {
        return Ok(new
        {
            message = "Request processed successfully",
            receivedData = request.Data,
            timestamp = DateTime.UtcNow
        });
    }
}
```

#### 4. ValidateTokenAttribute.cs - 多層驗證 Filter

```csharp
public class ValidateTokenAttribute : ActionFilterAttribute
{
    // ✅ Referer/Origin 白名單
    private static readonly string[] AllowedOrigins = new[]
    {
        "http://localhost:5073",
        "https://localhost:5073"
    };

    // ✅ User-Agent 黑名單（爬蟲工具）
    private static readonly string[] BotUserAgents = new[]
    {
        "curl/", "wget/", "scrapy", "python-requests", "java/",
        "go-http-client", "http.rb/", "axios/", "node-fetch"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var userAgent = request.Headers["User-Agent"].ToString();
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 第 1 層：驗證 User-Agent 黑名單
        if (!ValidateUserAgent(userAgent, logger))
        {
            context.Result = new ObjectResult(new { error = "Forbidden User-Agent" })
            {
                StatusCode = 403
            };
            return;
        }

        // 第 2 層：驗證 Referer Header
        if (!ValidateReferer(request, logger))
        {
            context.Result = new ObjectResult(new { error = "Invalid or missing Referer header" })
            {
                StatusCode = 403
            };
            return;
        }

        // 第 3 層：驗證 Origin Header
        if (!ValidateOrigin(request, logger))
        {
            context.Result = new ObjectResult(new { error = "Invalid or missing Origin header" })
            {
                StatusCode = 403
            };
            return;
        }

        // 第 4-8 層：驗證 Token
        var tokenService = context.HttpContext.RequestServices
            .GetRequiredService<ITokenProvider>();

        if (!request.Headers.TryGetValue("X-CSRF-Token", out var tokenValues))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing X-CSRF-Token header" });
            return;
        }

        var token = tokenValues.FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Empty X-CSRF-Token header" });
            return;
        }

        // 驗證 Token（包含過期、使用次數、User-Agent 一致性）
        if (!tokenService.ValidateToken(token, userAgent, ipAddress))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
            return;
        }

        base.OnActionExecuting(context);
    }
}
```

#### 5. TokenProvider.cs - Token 生成與驗證邏輯

```csharp
public class TokenProvider : ITokenProvider
{
    private readonly IMemoryCache _cache;

    public string GenerateToken(int maxUsageCount, int expirationMinutes, string userAgent, string ipAddress)
    {
        var token = Guid.NewGuid().ToString();
        var tokenData = new TokenData
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            MaxUsageCount = maxUsageCount,
            UsageCount = 0,
            UserAgent = userAgent, // ✅ 綁定 User-Agent
            IpAddress = ipAddress   // ✅ 綁定 IP（可選）
        };

        // ✅ 設定過期時間，自動清理
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenData.ExpiresAt
        };

        _cache.Set(token, tokenData, cacheOptions);
        return token;
    }

    public bool ValidateToken(string token, string userAgent, string ipAddress)
    {
        if (!_cache.TryGetValue(token, out TokenData? tokenData) || tokenData == null)
        {
            return false; // Token 不存在
        }

        if (DateTime.UtcNow > tokenData.ExpiresAt)
        {
            _cache.Remove(token);
            return false; // Token 過期
        }

        if (tokenData.UsageCount >= tokenData.MaxUsageCount)
        {
            _cache.Remove(token);
            return false; // 使用次數超過限制
        }

        // ✅ User-Agent 一致性檢查
        if (!string.IsNullOrEmpty(tokenData.UserAgent) &&
            !tokenData.UserAgent.Equals(userAgent, StringComparison.OrdinalIgnoreCase))
        {
            return false; // User-Agent 不一致，可能是 Token 被盜用
        }

        // ✅ 更新使用次數
        tokenData.UsageCount++;
        _cache.Set(token, tokenData, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenData.ExpiresAt
        });

        // ✅ 達到最大使用次數後移除
        if (tokenData.UsageCount >= tokenData.MaxUsageCount)
        {
            _cache.Remove(token);
        }

        return true;
    }
}
```

---

### Client Side 配置

#### 瀏覽器端（HTML + JavaScript）

```html
<!DOCTYPE html>
<html lang="zh-TW">
<head>
    <meta charset="UTF-8">
    <title>API Protected 測試頁面</title>
</head>
<body>
    <h1>API 安全測試</h1>
    <button onclick="testAPI()">取得 Token 並呼叫 Protected API</button>
    <div id="result"></div>

    <script>
        const API_BASE_URL = window.location.origin;

        async function testAPI() {
            try {
                // ✅ 步驟 1: 取得 Token
                const tokenResponse = await fetch(`${API_BASE_URL}/api/token`);
                const token = tokenResponse.headers.get('X-CSRF-Token');

                if (!token) {
                    document.getElementById('result').textContent = '❌ 無法取得 Token';
                    return;
                }

                console.log('✅ Token 取得成功:', token);

                // ✅ 步驟 2: 攜帶 Token 呼叫受保護端點
                const protectedResponse = await fetch(`${API_BASE_URL}/api/protected`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-CSRF-Token': token // ✅ Token 放在 Request Header
                    },
                    body: JSON.stringify({ data: 'test data' })
                });

                const result = await protectedResponse.json();

                if (protectedResponse.ok) {
                    document.getElementById('result').textContent =
                        `✅ 成功：${result.message}`;
                } else {
                    document.getElementById('result').textContent =
                        `❌ 失敗：${result.error}`;
                }
            } catch (error) {
                document.getElementById('result').textContent =
                    `❌ 錯誤：${error.message}`;
            }
        }
    </script>
</body>
</html>
```

#### 命令列工具（cURL）

由於 User-Agent 黑名單會拒絕 `curl/`，需要偽裝 User-Agent：

```bash
# ❌ 直接使用 curl 會被拒絕
curl -X POST http://localhost:5073/api/protected \
  -H "Content-Type: application/json" \
  -d '{"data":"test"}'

# ✅ 偽裝 User-Agent（僅供測試）
# 步驟 1: 取得 Token
TOKEN=$(curl -s -X GET http://localhost:5073/api/token \
  -H "User-Agent: Mozilla/5.0" \
  -i | grep -i "X-CSRF-Token" | cut -d' ' -f2 | tr -d '\r')

# 步驟 2: 使用 Token 呼叫 Protected API
curl -X POST http://localhost:5073/api/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0" \
  -H "X-CSRF-Token: $TOKEN" \
  -H "Referer: http://localhost:5073/" \
  -d '{"data":"test"}'
```

---

## 前端到後端互動的流程圖

```
┌─────────────────────────────────────────────────────────────────────┐
│                         前端（瀏覽器）                                │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                │ 步驟 1: GET /api/token
                                │ Headers: User-Agent: Mozilla/5.0
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         後端（ASP.NET Core）                         │
│                                                                       │
│  第 1 層：速率限制檢查（Token 生成：1 分鐘 5 次）                      │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 429 Too Many Requests                     │
│                                                                       │
│  TokenController.GetToken()                                          │
│  ├─ 生成 GUID Token                                                  │
│  ├─ 儲存 TokenData（包含 User-Agent、IP、過期時間、使用次數）         │
│  └─ 回傳 Response Header: X-CSRF-Token                               │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                │ 回傳 Token
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         前端（瀏覽器）                                │
│  ├─ 取得 Response Header: X-CSRF-Token                               │
│  └─ 儲存 Token 至變數                                                │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                │ 步驟 2: POST /api/protected
                                │ Headers:
                                │   - User-Agent: Mozilla/5.0
                                │   - X-CSRF-Token: <token>
                                │   - Referer: http://localhost:5073/
                                │ Body: {"data":"test"}
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         後端（ASP.NET Core）                         │
│                                                                       │
│  第 1 層：速率限制檢查（API 呼叫：10 秒 10 次）                        │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 429 Too Many Requests                     │
│                                                                       │
│  第 2 層：User-Agent 黑名單檢查                                       │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 403 Forbidden（curl/, wget/ 等）          │
│                                                                       │
│  第 3 層：Referer 白名單檢查                                          │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 403 Forbidden                             │
│                                                                       │
│  第 4 層：Token 存在性檢查                                            │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 401 Unauthorized                          │
│                                                                       │
│  第 5 層：Token 有效性檢查（是否在 Server 儲存）                       │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 401 Unauthorized                          │
│                                                                       │
│  第 6 層：Token 過期檢查                                              │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 401 Unauthorized                          │
│                                                                       │
│  第 7 層：Token 使用次數檢查                                          │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 401 Unauthorized（重放攻擊）              │
│                                                                       │
│  第 8 層：User-Agent 一致性檢查                                       │
│           ├─ PASS ──▶ 繼續                                            │
│           └─ FAIL ──▶ HTTP 401 Unauthorized（Token 被盜用）          │
│                                                                       │
│  ✅ 所有驗證通過                                                      │
│  └─ ProtectedController.PostData()                                   │
│      └─ 執行業務邏輯                                                  │
│      └─ 回傳 HTTP 200 OK                                             │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                │ 回傳結果
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         前端（瀏覽器）                                │
│  └─ 顯示成功訊息                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 做了那些實驗？

為了驗證防護機制的有效性，我設計了 **19 個自動化測試案例**，涵蓋 5 大場景：

### 場景 1: Token 基本功能驗證（3 個測試）

| 測試案例 | 目的 | 預期結果 |
|---------|------|---------|
| test-01-normal-flow.sh | 正常 Token 取得與使用 | ✅ HTTP 200 OK |
| test-02-token-expiration.sh | Token 過期測試（等待 61 秒） | ✅ HTTP 403 Forbidden |
| test-03-usage-limit.sh | Token 使用次數限制與重放攻擊防護 | ✅ 第 1 次成功，第 2 次失敗 |

### 場景 2: 安全防護驗證（10 個測試）

| 測試案例 | 目的 | 預期結果 |
|---------|------|---------|
| test-04-missing-token.sh | 無 Token 請求 | ✅ HTTP 403 Forbidden |
| test-05-invalid-token.sh | 無效 Token（偽造 GUID） | ✅ HTTP 403 Forbidden |
| test-06-ua-mismatch.sh | User-Agent 不一致 | ✅ HTTP 403 Forbidden |
| test-07-rate-limiting.sh | 速率限制（連續 6 次請求） | ✅ 第 6 次 HTTP 429 |
| test-08-injection-attack.sh | SQL Injection / XSS 攻擊測試 | ✅ HTTP 403 Forbidden |
| test-09-method-validation.sh | HTTP Method 限制（GET 存取 POST 端點） | ✅ HTTP 405 / 404 |
| test-10-content-type.sh | Content-Type 驗證 | ✅ HTTP 415 / 200 |
| test-16-empty-token.sh | 空字串 Token | ✅ HTTP 403 Forbidden |
| test-17-long-token.sh | 超長 Token（10000 字元） | ✅ HTTP 403 / 400 |
| test-18-malformed-token.sh | Token 格式錯誤（非 GUID） | ✅ HTTP 403 Forbidden |

### 場景 3: 瀏覽器整合測試（3 個測試）

| 測試案例 | 目的 | 預期結果 |
|---------|------|---------|
| test-11-browser-normal.spec.js | 瀏覽器正常流程（Playwright） | ✅ Token 取得 + API 呼叫成功 |
| test-12-browser-usage-limit.spec.js | 瀏覽器使用次數限制 | ✅ 前 2 次成功，第 3 次失敗 |
| test-13-cross-page.spec.js | 跨頁面 Token 使用（相同瀏覽器） | ✅ HTTP 200 OK（User-Agent 相同） |

### 場景 4: 直接 curl 攻擊測試（2 個測試）

| 測試案例 | 目的 | 預期結果 |
|---------|------|---------|
| test-11-direct-attack.sh | 直接攻擊 Protected API（無 Token） | ✅ HTTP 403 Forbidden |
| test-12-replay-attack.sh | 重放攻擊（重複使用 Token） | ✅ 第 1 次成功，第 2 次失敗 |

### 場景 5: 邊界條件測試（1 個測試）

| 測試案例 | 目的 | 預期結果 |
|---------|------|---------|
| test-19-missing-ua.sh | 缺少 User-Agent Header | ✅ HTTP 403 Forbidden |

---

## 測試結果

執行所有測試腳本後，結果如下：

```bash
# Linux/macOS
./tests/security/scripts/run-all-tests.sh

# Windows PowerShell
.\tests\security\scripts\run-all-tests.ps1
```

**測試摘要**：
- 總測試數：19 個
- ✅ 通過：19 個
- ❌ 失敗：0 個
- 通過率：100%

---

## 實驗心得與建議

### 實驗 1：User-Agent 黑名單的必要性

**實驗方法**：
- 使用 `curl` 直接呼叫 API（不偽裝 User-Agent）
- 偽裝 User-Agent 為 `Mozilla/5.0` 後重試

**結果**：
- ❌ 預設 `curl/` 被拒絕（HTTP 403）
- ✅ 偽裝後可通過（但仍需 Token）

**心得**：
User-Agent 黑名單能有效阻擋「懶人攻擊」（直接使用 curl 或 wget），但無法防止偽裝 User-Agent 的攻擊。因此必須搭配 Token 驗證。

---

### 實驗 2：Token 使用次數限制的效果

**實驗方法**：
- 設定 `maxUsage=1`，取得 Token 後連續呼叫 2 次

**結果**：
- ✅ 第 1 次：HTTP 200 OK
- ✅ 第 2 次：HTTP 401 Unauthorized

**心得**：
單次使用的 Token 能有效防止重放攻擊。即使攻擊者攔截到 Token，也只能使用一次。

---

### 實驗 3：速率限制的實際效果

**實驗方法**：
- 在 1 分鐘內快速發送 10 次 Token 請求

**結果**：
- ✅ 前 5 次：HTTP 200 OK
- ✅ 第 6 次起：HTTP 429 Too Many Requests

**心得**：
Fixed Window 速率限制能有效防止暴力破解，但要注意「時間窗口邊界問題」（例如在 00:59 發送 5 次，在 01:01 又可發送 5 次）。若需更精確的控制，可考慮 Sliding Window 或 Token Bucket 演算法。

---

### 實驗 4：User-Agent 綁定是否影響正常使用？

**實驗方法**：
- 瀏覽器 A 取得 Token
- 複製 Token 到瀏覽器 B（相同類型，如都是 Chrome）

**結果**：
- ✅ HTTP 200 OK（因為 User-Agent 完全相同）

**心得**：
User-Agent 綁定主要防止「跨客戶端盜用」（例如從瀏覽器偷 Token 到 curl），而非「跨瀏覽器實例」。若需更嚴格控制，可考慮加上 IP 地址綁定（但需注意 NAT 環境）。

---

### 實驗 5：Referer/Origin 驗證的局限性

**實驗方法**：
- 使用 curl 偽裝 Referer Header

```bash
curl -H "Referer: http://localhost:5073/" -H "X-CSRF-Token: $TOKEN" ...
```

**結果**：
- ✅ HTTP 200 OK（Referer 可被偽裝）

**心得**：
Referer/Origin 驗證僅能防止「瀏覽器同源政策」下的 CSRF 攻擊，無法防止命令列工具的偽裝。因此必須搭配 Token 驗證。

---

## 生產環境建議

若要部署到生產環境，建議額外加強以下設定：

### 1. 啟用 HTTPS 強制重導向

```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

### 2. 啟用 IP 地址綁定

在 `TokenProvider.ValidateToken` 中取消註解：

```csharp
// ✅ 取消註解以啟用 IP 檢查
if (!string.IsNullOrEmpty(tokenData.IpAddress) &&
    tokenData.IpAddress != ipAddress)
{
    return false;
}
```

**注意**：NAT 或代理伺服器環境下，IP 可能會變動，需謹慎評估。

### 3. 使用 Redis 替代 IMemoryCache

```csharp
// ❌ 開發環境
services.AddMemoryCache();

// ✅ 生產環境
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

**原因**：IMemoryCache 會在伺服器重啟時遺失，且無法跨多台伺服器共享。

### 4. 加強日誌監控

```csharp
_logger.LogWarning("Security Event: Token validation failed", new {
    EventType = "TokenValidationFailed",
    UserAgent = userAgent,
    IpAddress = ipAddress,
    Timestamp = DateTime.UtcNow
});
```

---

## 結論

本文分享了一套完整的 Web API 防濫用機制，包含：
- ✅ 8 層防護機制
- ✅ Token 生成、驗證、過期、使用次數限制
- ✅ User-Agent 黑名單與綁定
- ✅ Referer/Origin 白名單
- ✅ 速率限制
- ✅ 19 個自動化測試案例

透過多層防護，可以有效保護公開 API，防止被惡意濫用、爬蟲掃描或重放攻擊。

若有謬誤，煩請告知，新手發帖請多包涵 😊

---

## 範例程式碼

完整範例程式碼請參考：
- GitHub：[sample.dotblog/WebAPI/Lab.CSRF-2](https://github.com/yaochangsong/sample.dotblog/tree/master/WebAPI/Lab.CSRF-2)
- 測試腳本：`./tests/security/scripts/`
- 測試計畫：`./tests/security/security-test-plan.md`
- 安全機制說明：`./tests/security/SECURITY-MECHANISMS.md`

---

## 參考資料

- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [MDN: CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
