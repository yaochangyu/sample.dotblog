---
title: '[Security] SPA 如何用 PKCE + CSP 防止 Token 被竊'
abstract: >-
  SPA 身分驗證有個常見的陷阱：Token 存哪？存 localStorage 最方便，頁面重整也不會掉，但只要網站有一個 XSS 漏洞，攻擊者一行程式碼就能把 Token 帶走。存 Cookie 也不見得安全，沒設好一樣有風險。

  Authorization Code Flow + PKCE（Proof Key for Code Exchange）是目前的標準解法，解決的是授權碼被攔截後拿去換 Token 的問題。搭配 CSP（Content Security Policy）從瀏覽器端限制腳本來源，兩道防線合在一起，才算是比較完整的 SPA 安全架構。

  這篇文章用 ASP.NET Core Web API 自己實作授權伺服器，從頭走完整個 PKCE 流程，包含帳密驗證、Session Cookie 持久化、以及受保護的 API 端點。本文範例是 lab 性質的教學程式，重點是示範安全觀念，不是完整的正式 OAuth 產品。
keywords: ''
categories: ''
weblogName: Blog
postId: d2b310af-a400-4648-a191-cfcb3477e765
postDate: 2026-05-07T01:49:59.0000000
postStatus: draft
dontInferFeaturedImage: false
stripH1Header: true
---
# [Security] SPA 如何用 PKCE + CSP 防止 Token 被竊

---
## 前言

SPA 身分驗證有個常見的陷阱：Token 存哪？存 `localStorage` 最方便，頁面重整也不會掉，但只要網站有一個 XSS 漏洞，攻擊者一行程式碼就能把 Token 帶走。存 Cookie 也不見得安全，沒設好一樣有風險。

更早期的 OAuth 2.0 Implicit Flow 直接把 Token 放在 URL fragment 回傳，現在已不建議使用。目前的標準解法是 Authorization Code Flow + PKCE（Proof Key for Code Exchange），解決的是「授權碼被攔截後拿去換 Token」的問題。

搭配 CSP（Content Security Policy）從瀏覽器端限制腳本來源，兩道防線合在一起，才算是比較完整的 SPA 安全架構。這篇文章用 ASP.NET Core Web API 自己實作授權伺服器，從頭走完整個 PKCE 流程，包含帳密驗證、Session Cookie 持久化、以及受保護的 API 端點。

先講清楚：這個專案是 lab 性質的教學範例，目的是把 PKCE、CSP、Session Cookie 與 Bearer Token 的關係講清楚，而不是實作完整的 OAuth Authorization Server 產品。文中的程式碼仍刻意保留簡化，像是 client registry、使用者管理、持久化儲存與完整 scope 管理都不在這次範圍內。

---

## 開發環境

* Windows 11
* Chrome 124+
* 原生瀏覽器 Web Crypto API（無需安裝套件）
* .NET 10
* ASP.NET Core Web API

---

## PKCE 是什麼

PKCE 的核心概念很簡單：前端在發起登入前，先在本地產生一組隨機字串，叫做 `code_verifier`。把它做 SHA-256 雜湊之後得到 `code_challenge`，這個雜湊值才送給授權伺服器。

等到拿到 Authorization Code 要換 Token 時，再把原始的 `code_verifier` 送過去，授權伺服器會重新計算一次 `SHA256(verifier)`，比對是否和當初的 `code_challenge` 相符，相符才核發 Token。

如此一來，就算授權碼在傳輸途中被攔截，攻擊者沒有 `code_verifier` 也換不到 Token。

整個流程如下：

```

sequenceDiagram
    autonumber
    participant Browser as 瀏覽器
    participant SPA as 前端 (SPA)
    participant Auth as 授權伺服器
    participant API as 後端 API

    Browser->>SPA: 載入頁面
    SPA->>Auth: GET /authorize/session（帶 sid Cookie）

    alt 首次登入（無有效 Session）
        Auth-->>SPA: 401
        Note over SPA: 顯示帳密輸入表單
        Browser->>SPA: 輸入帳號密碼
        SPA->>SPA: 產生 code_verifier 與 code_challenge
        SPA->>Auth: POST /authorize（帳密 + code_challenge）
        Auth->>Auth: 驗證帳密通過，建立 Session
        Auth-->>SPA: Set-Cookie: sid（HttpOnly）；回傳 Authorization Code
        SPA->>Auth: POST /token（code + code_verifier）
        Auth->>Auth: 驗證 SHA256(verifier) == challenge
        Auth-->>SPA: 回傳 Access Token
    else Session 有效（靜默換 Token）
        Auth-->>SPA: 200 { username }
        SPA->>SPA: 自動產生 code_verifier 與 code_challenge
        SPA->>Auth: POST /authorize（帶 Cookie，不帶帳密）
        Auth-->>SPA: Session 有效，回傳 Authorization Code
        SPA->>Auth: POST /token（code + code_verifier）
        Auth->>Auth: 驗證 SHA256(verifier) == challenge
        Auth-->>SPA: 回傳 Access Token
    end

    SPA->>API: GET /api/me（Authorization: Bearer Token）
    API-->>SPA: 200 { username, message }
```

* * *

## 用 Web Crypto API 產生 PKCE 密語

瀏覽器原生的 `window.crypto` 就可以做這件事，不需要任何外部套件。

產生 `code_verifier` 與 `code_challenge` 的方式如下：

```javascript
function base64UrlEncode(array) {
    return btoa(String.fromCharCode.apply(null, array))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
}

async function generatePkce() {
    // 產生 32 bytes 的隨機值作為 code_verifier
    const array = new Uint8Array(32);
    window.crypto.getRandomValues(array);
    const verifier = base64UrlEncode(array);

    // 對 verifier 做 SHA-256 雜湊，得到 code_challenge
    const hash = await window.crypto.subtle.digest(
        'SHA-256',
        new TextEncoder().encode(verifier)
    );
    const challenge = base64UrlEncode(new Uint8Array(hash));

    return { verifier, challenge };
}
```

NOTE：`code_verifier` 只存在記憶體（JavaScript 變數），**不要**寫進 `localStorage`、`sessionStorage` 或 Cookie。它是 PKCE 流程中的一次性密語，用完即丟，沒有持久化的必要。

* * *

## CSP 是什麼，為什麼需要它

PKCE 防的是授權碼被攔截；但如果網站本身有 XSS 漏洞，攻擊者可以直接在你的網頁裡執行任意 JavaScript，包括讀取存在記憶體的 Token。

CSP（Content Security Policy）是由伺服器透過 HTTP Response Header 告訴瀏覽器：「這個頁面只允許載入哪些來源的資源。」如果惡意腳本的來源不在白名單內，瀏覽器就會直接阻擋。

設定範例如下（HTTP Header）：

```
Content-Security-Policy: default-src 'self'; script-src 'self' https://trusted-cdn.com
```

這樣即便攻擊者設法注入外部腳本，或是插入 inline `<script>`，瀏覽器也會依政策拒絕執行。

這個 lab 範例現在會真的由 ASP.NET Core 回傳 CSP header。模擬器頁面也把原本的 inline JavaScript 移到獨立的 `emulator.js`，所以可以在 `script-src 'self'` 的前提下，直接示範 inline XSS 被 CSP 擋掉的效果。

CSP 的防禦決策流程如下：

```

flowchart TD
    A[駭客嘗試進行 XSS 攻擊，注入惡意腳本] --> B{伺服器是否設定嚴格的 CSP?}
    B -- 否 --> C[瀏覽器執行惡意腳本]
    C --> D[讀取記憶體或 Storage]
    D --> E((Token 遭竊))

    B -- 是 --> F[瀏覽器依據白名單阻擋未授權腳本]
    F --> G((攻擊失敗，Token 安全))

    style E fill:#fca5a5,stroke:#ef4444,stroke-width:2px
    style G fill:#a7f3d0,stroke:#10b981,stroke-width:2px
```

* * *

## 授權伺服器（ASP.NET Core Web API）

### 身分驗證關卡：UserStore

在真實的 OAuth 流程中，授權伺服器必須先確認「是哪個使用者在要求授權」，才能核發 Authorization Code。這個範例仍然用 in-memory 的方式存放帳號，但密碼不再用單純的 SHA-256，而是改成 .NET 內建的 `PasswordHasher<TUser>`：

```csharp
public class UserStore
{
    private readonly Dictionary<string, User> _users;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UserStore()
    {
        _users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = CreateUser("alice", "password123"),
            ["bob"]   = CreateUser("bob", "password456"),
        };
    }

    public bool Validate(string username, string password)
    {
        if (!_users.TryGetValue(username, out var user))
            return false;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
```

NOTE：這仍然只是示範帳號存在記憶體的 lab，用來說明流程而已；但至少密碼雜湊方式已經避免把單純的 SHA-256 當成正式範例。

### Session Cookie 持久化

每次都要輸入帳密不是真實場景。常見的解法是登入成功後建立 Server 端 Session，並透過 `HttpOnly` Cookie 讓瀏覽器自動攜帶，後續的 `/authorize` 只要 Session 有效就跳過帳密輸入。

流程如下：

```
第一次：POST /authorize { username, password, client_id, redirect_uri, code_challenge }
  → 驗帳密通過 → 建立 Session → Set-Cookie: sid=xxx; HttpOnly; Secure; SameSite=Strict
  → 回傳 code

之後：POST /authorize { client_id, redirect_uri, code_challenge }（帶 Cookie，不帶帳密）
  → 查 Cookie 中的 sid → Session 有效 → 直接回傳 code
  → Session 過期或不存在 → 401，需重新輸入帳密
```

`/authorize` 的驗證邏輯如下：

```csharp
[HttpPost]
public IActionResult Post([FromBody] AuthorizeRequest request)
{
    if (!OAuthClientPolicy.IsValid(request.ClientId, request.RedirectUri, Request))
        return BadRequest("client_id 或 redirect_uri 不合法");

    string username;

    if (TryGetValidSession(out var session))
    {
        // Session 有效，跳過帳密輸入
        username = session!.Username;
    }
    else
    {
        if (!users.Validate(request.Username, request.Password))
            return Unauthorized("帳號或密碼錯誤");

        username = request.Username;

        // 登入成功，建立 Session 並寫入 HttpOnly Cookie
        var newSession = sessions.Create(username);
        Response.Cookies.Append("sid", newSession.SessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires  = newSession.ExpiresAt,
            Path = "/"
        });
    }

    var code = store.Save(new AuthorizationCode
    {
        ClientId = request.ClientId,
        RedirectUri = request.RedirectUri,
        CodeChallenge = request.CodeChallenge,
        Username = username
    });
    return Ok(new { code, username });
}
```

NOTE：Cookie 設定 `HttpOnly` 很重要，但還不夠；若沒有 `Secure` 搭配 HTTPS，Session ID 仍可能在傳輸途中被攔截。

前端產好 `code_challenge` 之後，要有人接收、暫存，並在後續驗證 `code_verifier`。這個角色就是授權伺服器，這裡用 ASP.NET Core Web API 自己實作驗證邏輯。

### 專案結構

```
AuthServer/
├── Controllers/
│   ├── AuthorizeController.cs   # POST /authorize、GET /authorize/session
│   ├── TokenController.cs       # POST /token
│   ├── MeController.cs          # GET /api/me（受保護端點）
│   └── LogoutController.cs      # POST /logout
├── Models/
│   ├── AuthorizationCode.cs
│   ├── AuthorizeRequest.cs
│   ├── TokenRequest.cs
│   └── TokenResponse.cs
├── Services/
│   ├── AuthorizationCodeStore.cs  # In-Memory 暫存（一次性）
│   ├── AccessTokenStore.cs        # Token → Username 對應
│   ├── OAuthClientPolicy.cs       # client_id / redirect_uri 驗證
│   ├── SessionStore.cs            # Session 管理
│   ├── UserStore.cs               # 帳號密碼驗證
│   └── PkceService.cs             # PKCE 驗證邏輯
├── wwwroot/
│   ├── emulator.html              # PKCE + CSP 互動模擬器
│   ├── emulator.js                # 模擬器腳本（獨立檔案，避免 inline script）
│   └── flow.html                  # 流程圖（Mermaid.js）
└── Program.cs
```

### PKCE 驗證核心：PkceService

這裡是整個 Auth Server 最關鍵的一段，`SHA256(codeVerifier)` 做 Base64Url 編碼後，和前端送來的 `codeChallenge` 比對：

```csharp
public class PkceService
{
    public bool Verify(string codeVerifier, string codeChallenge)
    {
        var computed = ComputeChallenge(codeVerifier);
        // 使用固定時間比較，避免 timing attack
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(codeChallenge)
        );
    }

    private static string ComputeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
```

NOTE：比對時改用 `CryptographicOperations.FixedTimeEquals`，執行時間不會因字串比對提早結束而洩漏資訊，可以防止 timing attack。

### Authorization Code 一次性暫存

Authorization Code 用完就必須移除，不能重複使用：

```csharp
public class AuthorizationCodeStore
{
    private readonly ConcurrentDictionary<string, AuthorizationCode> _store = new();

    public string Save(AuthorizationCode code)
    {
        var key = GenerateCode();
        _store[key] = code;
        return key;
    }

    // 取出後立即移除，確保 code 一次性使用
    public AuthorizationCode? TakeAndRemove(string code)
    {
        _store.TryRemove(code, out var entry);
        return entry;
    }
}
```

### /token 端點

收 `code` + `code_verifier`，驗證通過才核發 Token；這次也會額外驗證 `client_id` 與 `redirect_uri` 是否和授權碼建立時一致：

```csharp
[HttpPost]
public IActionResult Post([FromBody] TokenRequest request)
{
    var entry = store.TakeAndRemove(request.Code);
    if (entry is null)
        return BadRequest("authorization_code 不存在或已使用");

    if (entry.IsExpired)
        return BadRequest("authorization_code 已過期");

    if (entry.ClientId != request.ClientId || entry.RedirectUri != request.RedirectUri)
        return BadRequest("client_id 或 redirect_uri 與 authorization_code 不符");

    if (!pkce.Verify(request.CodeVerifier, entry.CodeChallenge))
        return Unauthorized("PKCE 驗證失敗：verifier 與 challenge 不符");

    var token = $"eyJ.{Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=')}.SIG";

    // 核發後存入 store，供 /api/me 驗證使用
    tokens.Save(token, entry.Username);

    return Ok(new TokenResponse
    {
        AccessToken = token,
        ExpiresIn = tokens.ExpiresInSeconds
    });
}
```

NOTE：`expires_in` 現在會對應到伺服器端實際的 Token 到期時間，而不是只有文件寫了、程式卻沒檢查。

* * *

## 受保護的 API 端點（GET /api/me）

拿到 Access Token 之後，終於可以做點有意義的事了——呼叫需要身分驗證的 API。

`/api/me` 從 `Authorization` header 取出 Bearer Token，查 `AccessTokenStore` 確認有效後回傳使用者資訊：

```csharp
[HttpGet]
public IActionResult Get()
{
    var token = ExtractBearerToken();
    if (token is null)
        return Unauthorized("缺少 Authorization Bearer Token");

    var username = tokens.GetUsername(token);
    if (username is null)
        return Unauthorized("Token 無效或已過期");

    return Ok(new
    {
        username,
        message = $"哈囉，{username}！你已通過身分驗證。",
        issuedAt = DateTime.UtcNow
    });
}
```

前端呼叫時把 Token 放進 header，如下：

```javascript
const res = await fetch(`${AUTH_SERVER}/api/me`, {
    headers: { 'Authorization': `Bearer ${state.token}` }
});
```

NOTE：Token 只存在前端記憶體，每次呼叫 API 都要自己帶上去。這和 Cookie 的「自動攜帶」不同，是刻意為之——讓開發者明確控制哪些請求需要授權。

* * *

## 用模擬器驗證兩道防線

模擬器（`emulator.html`）放在 `AuthServer/wwwroot/`，透過 `app.UseStaticFiles()` 和授權伺服器一起啟動，直接用瀏覽器開啟 `https://localhost:7070/emulator.html` 即可。

這樣做的原因是 `SameSite=Strict` + `Secure` 的 Session Cookie 只會在 HTTPS 同源請求時被帶上。如果用 `file://` 開啟頁面，origin 是 `null`，瀏覽器不會附帶 Cookie，Session 機制就完全失效了。

頁面載入時，如果偵測到有效的 Session Cookie，會**自動靜默完成整個 PKCE 流程**（產生 verifier/challenge → 帶 Cookie 取得 Authorization Code → 換 Token），不需要使用者手動操作，Token 恢復後直接可以呼叫受保護 API。

可以用按鈕一步步走完整個 PKCE 流程，最後觸發 inline XSS 攻擊，觀察 CSP 開啟和關閉時有什麼差異。模擬器上的開關會重新載入頁面，切換成「有真正 CSP header」或「無 CSP header」兩種模式。

開啟 CSP 防護時，觸發攻擊的日誌輸出如下：

```
[09:30:01] --- 遭受 XSS 攻擊 ---
[09:30:01] Hacker -> SPA: 注入 inline <script> 嘗試竊取記憶體中的 Token
[09:30:02] SPA (CSP): ⛔ 已阻擋 inline script。違規指令：script-src-elem
[09:30:02] 防禦成功，記憶體中的 Token 安全無虞。
```

關閉 CSP 後再試一次：

```
[09:31:15] --- 遭受 XSS 攻擊 ---
[09:31:15] Hacker -> SPA: 注入 inline <script> 嘗試竊取記憶體中的 Token
[09:31:16] SPA: 惡意腳本成功執行！
[09:31:16] Hacker: 已竊取記憶體資料 Token: ey.abc123.JWT
```

差異一目瞭然。

* * *

## 心得

PKCE 的概念乍看複雜，但實際上就是「先送雜湊、後送原文、讓伺服器比對」這件事，理解之後其實滿直覺的。

Token 存記憶體而不是 `localStorage` 這點讓我想了一下。記憶體存活時間只到頁面關閉，代表使用者重新整理後 Token 就不見了。不過搭配 Session Cookie（`HttpOnly` + `Secure` + `SameSite=Strict`），可以在頁面載入時靜默重跑一遍 PKCE 流程，Server 端確認 Session 有效就直接核發新的短效 Token，使用者根本感覺不到。這種做法比把 Token 丟進 `localStorage` 安全，體驗上也不差。實務上還可以搭配 Refresh Token Rotation 或 BFF（Backend for Frontend）架構，這些留到之後再研究。

個人覺得 CSP 是被低估的防護手段，很多專案連 CSP Header 都沒設，XSS 一打就穿。兩道防線一起做，總比只做一道好。

* * *

## 範例位置

```
sample.dotblog/Security/Lab.PKCE/
├── AuthServer/
│   └── wwwroot/
│       ├── emulator.html   # PKCE + CSP 互動模擬器
│       ├── emulator.js     # 模擬器腳本
│       └── flow.html       # PKCE 與 CSP 流程圖（Mermaid.js）
└── SPA-PKCE-CSP-Security.md
```

啟動 AuthServer 後用瀏覽器開啟：
- `https://localhost:7070/emulator.html` — 互動模擬器
- `https://localhost:7070/flow.html` — 流程圖

若有謬誤，煩請告知，新手發帖請多包涵
