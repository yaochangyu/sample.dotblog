# [ASP.NET] 用 HybridCache + Cookie 取代 Session

ASP.NET Framework 的 Session 有排他鎖，同一個使用者的 Request 會排隊。ASP.NET Core 的 Session 不會排隊，但底層是 `IDistributedCache`，每次存取直接打 Redis，沒有 L1 記憶體快取，高流量有擊穿風險。

這篇用 `HybridCache` + `Cookie` 實作 `SessionCacheProvider`，用起來跟 `Session["key"]` 一樣，同時支援 .NET Framework 4.8 和 .NET 10。

## 開發環境

- Windows 11
- .NET 10 / .NET Framework 4.8
- Microsoft.Extensions.Caching.Hybrid 10.4.0
- Reqnroll + xUnit (BDD)

## 設計概念

- **Cookie** 存 Session ID（GUID）
- **HybridCache** 存 Session 資料，key 格式 `session:{sessionId}:{key}`
- HybridCache 提供 **L1 記憶體 + L2 分散式快取**，解決擊穿問題

```
Browser Cookie (SessionCacheId=xxx)
        │
        ▼
SessionCacheProvider（建立/取得 Session ID）
        │
        ▼
SessionObject（this["key"] 索引器）
        │
        ▼
HybridCache（L1 + L2）
```

## 專案結構

```
Lab.SessionCacheProvider/
├── Lab.SessionCacheProvider/
│   ├── ICookieAccessor.cs
│   ├── AspNetCookieAccessor.cs          // .NET Framework
│   ├── AspNetCoreCookieAccessor.cs      // .NET Core
│   ├── SessionObject.cs
│   ├── SessionCacheProvider.cs
│   ├── CacheSession.cs
│   └── SessionCacheProviderExtensions.cs
└── Lab.SessionCacheProvider.Tests/
    ├── Features/
    │   ├── SessionObject.feature
    │   ├── SessionCacheProvider.feature
    │   ├── CacheSession.feature
    │   └── TestServerIntegration.feature
    └── ...
```

Multi-Target，同時支援 `net48` 和 `net10.0`：

```xml
<TargetFrameworks>net48;net10.0</TargetFrameworks>

<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="10.4.0" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net48'">
  <Reference Include="System.Web" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

## 實作

### ICookieAccessor

用介面抽離平台差異，核心邏輯不需要 `#if` 條件編譯。

```csharp
public static class SessionCacheConstants
{
    public const string CookieKey = "SessionCacheId";
}

public interface ICookieAccessor
{
    string? GetSessionId();
    void SetSessionId(string sessionId);
}
```

### AspNetCoreCookieAccessor

透過 `IHttpContextAccessor` 存取 Cookie，`HttpContext.Items` 在同一個 Request 內快取 Session ID。

```csharp
public class AspNetCoreCookieAccessor : ICookieAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AspNetCoreCookieAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetSessionId()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext 為 null，無法存取 Cookie。");

        if (context.Items.TryGetValue(SessionCacheConstants.CookieKey, out var cachedId)
            && cachedId is string id)
        {
            return id;
        }

        if (context.Request.Cookies.TryGetValue(SessionCacheConstants.CookieKey, out var existingId))
        {
            context.Items[SessionCacheConstants.CookieKey] = existingId;
            return existingId;
        }

        return null;
    }

    public void SetSessionId(string sessionId)
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext 為 null，無法存取 Cookie。");

        context.Response.Cookies.Append(SessionCacheConstants.CookieKey, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Path = "/"
        });
        context.Items[SessionCacheConstants.CookieKey] = sessionId;
    }
}
```

NOTE：`HttpContext.Items` 的生命週期是單次 Request，避免多次存取 `Session` 屬性時重複產生 GUID。

### AspNetCookieAccessor

.NET Framework 注入 `HttpContextBase`，不直接用 `HttpContext.Current`，方便測試。

```csharp
public class AspNetCookieAccessor : ICookieAccessor
{
    private readonly HttpContextBase _httpContext;

    public AspNetCookieAccessor(HttpContextBase httpContext)
    {
        _httpContext = httpContext;
    }

    public string? GetSessionId()
    {
        if (_httpContext.Items[SessionCacheConstants.CookieKey] is string cachedId)
        {
            return cachedId;
        }

        var cookie = _httpContext.Request.Cookies[SessionCacheConstants.CookieKey];
        if (cookie != null)
        {
            _httpContext.Items[SessionCacheConstants.CookieKey] = cookie.Value;
            return cookie.Value;
        }

        return null;
    }

    public void SetSessionId(string sessionId)
    {
        _httpContext.Response.Cookies.Set(new HttpCookie(SessionCacheConstants.CookieKey, sessionId)
        {
            HttpOnly = true,
            Path = "/"
        });
        _httpContext.Items[SessionCacheConstants.CookieKey] = sessionId;
    }
}
```

### SessionObject

提供 `this["key"]` 索引器，用起來跟 `Session["key"]` 一樣。

```csharp
public class SessionObject
{
    private const string KeyPrefix = "session";

    private readonly HybridCache _cache;
    private readonly string _sessionId;
    private readonly HybridCacheEntryOptions _entryOptions;

    public SessionObject(HybridCache cache, string sessionId, HybridCacheEntryOptions entryOptions)
    {
        _cache = cache;
        _sessionId = sessionId;
        _entryOptions = entryOptions;
    }

    public object? this[string key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }

    public T? Get<T>(string key)
    {
        var cacheKey = BuildCacheKey(key);
        return _cache.GetOrCreateAsync(
            cacheKey,
            _ => new ValueTask<T?>(default(T)),
            _entryOptions
        ).AsTask().GetAwaiter().GetResult();
    }

    public void Set<T>(string key, T value)
    {
        var cacheKey = BuildCacheKey(key);
        _cache.SetAsync(cacheKey, value, _entryOptions)
            .AsTask().GetAwaiter().GetResult();
    }

    public void Remove(string key)
    {
        var cacheKey = BuildCacheKey(key);
        _cache.RemoveAsync(cacheKey)
            .AsTask().GetAwaiter().GetResult();
    }

    private string BuildCacheKey(string key) => $"{KeyPrefix}:{_sessionId}:{key}";

    // 省略 Async 版本...
}
```

### SessionCacheProvider

從 Cookie 取得或建立 Session ID，不需要條件編譯，平台差異由 `ICookieAccessor` 處理。

```csharp
public class SessionCacheProvider
{
    private readonly HybridCache _cache;
    private readonly ICookieAccessor _cookieAccessor;
    private readonly HybridCacheEntryOptions _entryOptions;

    public SessionCacheProvider(
        HybridCache cache,
        ICookieAccessor cookieAccessor,
        HybridCacheEntryOptions entryOptions)
    {
        _cache = cache;
        _cookieAccessor = cookieAccessor;
        _entryOptions = entryOptions;
    }

    public SessionObject Session
    {
        get
        {
            var sessionId = GetOrCreateSessionId();
            return new SessionObject(_cache, sessionId, _entryOptions);
        }
    }

    private string GetOrCreateSessionId()
    {
        var existingId = _cookieAccessor.GetSessionId();
        if (existingId != null)
        {
            return existingId;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _cookieAccessor.SetSessionId(sessionId);
        return sessionId;
    }
}
```

### CacheSession

靜態便利類別，提供 `CacheSession.Current["key"]`。

**ASP.NET Core：**

```csharp
CacheSession.Current["UserName"] = "John";
var name = CacheSession.Current["UserName"];
```

**ASP.NET Framework：**

```csharp
// Application_Start 初始化
CacheSession.Initialize(cache, entryOptions);

// 使用方式一樣
CacheSession.Current["UserName"] = "John";
```

### SessionCacheProviderExtensions

DI 註冊，風格對齊 `AddHybridCache(Action<HybridCacheOptions>)`。

```csharp
public static class SessionCacheProviderExtensions
{
    public static IServiceCollection AddSessionCacheProvider(
        this IServiceCollection services,
        Action<HybridCacheEntryOptions>? setupAction = null)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(20),
            LocalCacheExpiration = TimeSpan.FromMinutes(5)
        };

        setupAction?.Invoke(entryOptions);

        services.AddHttpContextAccessor();
        services.AddScoped<ICookieAccessor, AspNetCoreCookieAccessor>();
        services.AddScoped<SessionCacheProvider>();
        services.AddSingleton(entryOptions);
        return services;
    }

    public static IApplicationBuilder UseSessionCache(this IApplicationBuilder app)
    {
        var accessor = app.ApplicationServices
            .GetRequiredService<IHttpContextAccessor>();
        CacheSession.SetHttpContextAccessor(accessor);
        return app;
    }
}
```

使用方式：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(20),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});

// 使用預設值
builder.Services.AddSessionCacheProvider();

// 或自訂
builder.Services.AddSessionCacheProvider(options =>
{
    options.Expiration = TimeSpan.FromMinutes(30);
    options.LocalCacheExpiration = TimeSpan.FromMinutes(10);
});

var app = builder.Build();
app.UseSessionCache();
```

NOTE：`Expiration` 是 L2（分散式快取）過期時間，`LocalCacheExpiration` 是 L1（記憶體）過期時間，L1 通常設比較短。

## BDD 測試

Reqnroll + xUnit，測試專案同樣 Multi-Target（`net48;net10.0`）。

### SessionObject

```gherkin
Feature: SessionObject

    Scenario: 透過索引器設定與取得值
        Given 一個 SessionObject 實例
        When 設定 key "UserName" 的值為 "John"
        Then key "UserName" 的值應為 "John"

    Scenario: 取得不存在的 key 回傳 null
        Given 一個 SessionObject 實例
        Then key "NonExisting" 的值應為 null

    Scenario: 設定 null 等同移除
        Given 一個 SessionObject 實例
        And 設定 key "UserName" 的值為 "John"
        When 設定 key "UserName" 的值為 null
        Then key "UserName" 的值應為 null

    Scenario: 透過 Remove 移除值
        Given 一個 SessionObject 實例
        And 設定 key "UserName" 的值為 "John"
        When 移除 key "UserName"
        Then key "UserName" 的值應為 null

    Scenario: 設定與取得強型別值
        Given 一個 SessionObject 實例
        When 設定 key "Age" 的整數值為 42
        Then key "Age" 的整數值應為 42

    Scenario: 覆寫既有的值
        Given 一個 SessionObject 實例
        And 設定 key "UserName" 的值為 "John"
        When 設定 key "UserName" 的值為 "Jane"
        Then key "UserName" 的值應為 "Jane"
```

### TestServer 整合測試

用 `WebApplicationFactory` 建立 TestServer，驗證 Cookie 寫入、跨 Request 取值、Session 隔離。

```gherkin
Feature: TestServer 整合測試

    Scenario: 首次請求自動建立 SessionCacheId cookie
        Given 一個 TestServer 應用程式
        When 發送 GET 請求到 "/api/session/get?key=Name"
        Then Response 應包含 "SessionCacheId" cookie

    Scenario: 跨請求透過 cookie 取回先前設定的值
        Given 一個 TestServer 應用程式
        When 發送 POST 請求到 "/api/session/set" 並帶入 key "City" 值 "Taipei"
        And 帶著相同的 cookie 發送 GET 請求到 "/api/session/get?key=City"
        Then Response 的內容應為 "Taipei"

    Scenario: 不同 Session 之間資料互不干擾
        Given 一個 TestServer 應用程式
        When 發送 POST 請求到 "/api/session/set" 並帶入 key "Token" 值 "AAA"
        And 不帶 cookie 發送 GET 請求到 "/api/session/get?key=Token"
        Then Response 的內容應為空字串

    Scenario: 透過 CacheSession.Current 靜態存取設定與取得值
        Given 一個 TestServer 應用程式
        When 發送 POST 請求到 "/api/session/set-static" 並帶入 key "Lang" 值 "zh-TW"
        And 帶著相同的 cookie 發送 GET 請求到 "/api/session/get-static?key=Lang"
        Then Response 的內容應為 "zh-TW"
```

TestServer Fixture：

```csharp
public class TestWebServer : WebApplicationFactory<TestWebApp>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var app = TestWebApp.CreateApp(
            Array.Empty<string>(),
            b => b.WebHost.UseTestServer());

        app.Start();
        return app;
    }
}
```

## 運行結果

net48 排除 ASP.NET Core 專用的 Feature，只跑 SessionObject 和 SessionCacheProvider：

```
Passed!  - Failed: 0, Passed:  9, Skipped: 0, Total:  9  (net48)
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16  (net10.0)
```

### net48 排除 Feature 的注意事項

Reqnroll 在 `Reqnroll.Tools.MsBuild.Generation.props` 以 `<ReqnrollFeatureFile Include="**\*.feature">` 自動掃描所有 feature 檔，單純 `<None Remove>` 無效，要加上 `<ReqnrollFeatureFile Remove>`：

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net48'">
  <Compile Remove="StepDefinitions\CacheSessionStepDefinitions.cs" />
  <Compile Remove="StepDefinitions\TestServerIntegrationStepDefinitions.cs" />
  <Compile Remove="Support\TestWebApp.cs" />
  <Compile Remove="Support\TestWebServer.cs" />
  <Compile Remove="Support\AssemblyInfo.cs" />
  <None Remove="Features\CacheSession.feature" />
  <None Remove="Features\TestServerIntegration.feature" />
  <ReqnrollFeatureFile Remove="Features\CacheSession.feature" />
  <ReqnrollFeatureFile Remove="Features\TestServerIntegration.feature" />
</ItemGroup>
```

## 範例位置

<https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.SessionCacheProvider>

若有謬誤，煩請告知，新手發帖請多包涵

Microsoft MVP Award 2010~2017 C# 第四季
Microsoft MVP Award 2018~2022 .NET
