# Lab.SessionCacheProvider

使用 HybridCache + Cookie 取代 ASP.NET Session，解決 Session 互斥鎖導致同一使用者的 Request 排隊問題。

參考：[Session 不上鎖的低調做法](https://blog.darkthread.net/blog/session-alternative/)

## 支援框架

- .NET Framework 4.8
- .NET 10

## 為什麼需要這個？

| 問題 | .NET Framework | .NET 10 |
|---|---|---|
| Session 造成 request 排隊 | **有**（互斥鎖） | 無（已改善） |
| Session 每次存取都打 Redis | 有 | **有**（無 L1 快取） |

- **.NET Framework**：解決 request 排隊 + 加入 L1 快取
- **.NET 10**：原生 Session 不會排隊，但缺少 L1 快取層，高流量下 Redis 可能被擊穿。HybridCache 提供 L1 in-memory + L2 分散式雙層快取

## 設計概念

| 原本 ASP.NET Session | SessionCacheProvider |
|---|---|
| 整個 Request 鎖定 Session | 僅在資料讀寫時短暫操作 |
| MemoryCache / StateServer | HybridCache（L1 in-memory + L2 分散式） |
| Session Cookie（ASP.NET 管理） | 自訂 Cookie 儲存 SessionId（GUID） |

### 架構

```
CacheSession                 — 靜態便利類別，提供 CacheSession.Current["key"] 存取

ICookieAccessor              — Cookie 存取抽象介面
├── AspNetCookieAccessor     — .NET Framework 實作（注入 HttpContextBase）
└── AspNetCoreCookieAccessor — .NET 10 實作（注入 IHttpContextAccessor）

SessionCacheProvider         — 核心類別，管理 SessionId 與 Session 屬性
SessionObject                — 索引器類別，提供 this[key] 存取，貼近 Session 使用習慣
SessionCacheConstants        — 共用常數（CookieKey）
```

## 使用方式

### 靜態存取（推薦，最貼近原生 Session）

```csharp
// 用法幾乎等同 Session["key"]
CacheSession.Current["UserName"] = "John";
var name = CacheSession.Current["UserName"];
CacheSession.Current.Remove("UserName");

// 強型別存取
CacheSession.Current.Set("Age", 30);
var age = CacheSession.Current.Get<int>("Age");
```

#### .NET 10 設定

```csharp
// Program.cs
builder.Services.AddHybridCache();
builder.Services.AddSessionCacheProvider();

var app = builder.Build();
app.UseSessionCache();  // 啟用 CacheSession.Current
```

#### .NET Framework 設定

```csharp
// Global.asax Application_Start
var entryOptions = new HybridCacheEntryOptions
{
    Expiration = TimeSpan.FromMinutes(20),
    LocalCacheExpiration = TimeSpan.FromMinutes(5)
};
CacheSession.Initialize(hybridCache, entryOptions);
```

### DI 注入（.NET 10）

```csharp
// 自訂 L1/L2 過期時間
builder.Services.AddSessionCacheProvider(options =>
{
    options.Expiration = TimeSpan.FromMinutes(30);           // L2 分散式快取
    options.LocalCacheExpiration = TimeSpan.FromMinutes(10); // L1 in-memory
});
```

```csharp
public class HomeController : Controller
{
    private readonly SessionCacheProvider _sessionCacheProvider;

    public HomeController(SessionCacheProvider sessionCacheProvider)
    {
        _sessionCacheProvider = sessionCacheProvider;
    }

    public IActionResult Index()
    {
        _sessionCacheProvider.Session["UserName"] = "John";
        var name = _sessionCacheProvider.Session["UserName"];
        return View();
    }
}
```

## 預設值

| 設定 | 預設值 | 說明 |
|---|---|---|
| `Expiration`（L2） | 20 分鐘 | 分散式快取過期時間 |
| `LocalCacheExpiration`（L1） | 5 分鐘 | in-memory 快取過期時間，較短以降低多節點資料不一致 |
| Cookie Name | `SessionCacheId` | 儲存 SessionId 的 Cookie 名稱 |

## 測試

使用 Reqnroll（BDD）+ xUnit，共 12 個測試案例：

```bash
dotnet test
```

### 測試涵蓋範圍

- **SessionObject**：索引器 get/set、null 移除、Remove、型別化存取、覆寫值
- **SessionCacheProvider**：Cookie 新建/重用 SessionId、Session 屬性整合存取
- **CacheSession**：靜態 Current 屬性的 get/set/remove

## 專案結構

```
Lab.SessionCacheProvider/
├── Lab.SessionCacheProvider/          # Class Library（net48;net10.0）
│   ├── ICookieAccessor.cs             # Cookie 存取介面 + SessionCacheConstants
│   ├── AspNetCookieAccessor.cs        # .NET Framework 實作
│   ├── AspNetCoreCookieAccessor.cs    # .NET 10 實作
│   ├── SessionObject.cs              # 索引器類別
│   ├── SessionCacheProvider.cs        # 核心類別
│   ├── CacheSession.cs               # 靜態便利類別
│   └── SessionCacheProviderExtensions.cs  # DI 擴充方法 + UseSessionCache()
└── Lab.SessionCacheProvider.Tests/    # BDD 測試專案
    ├── Features/                      # Gherkin Feature 檔案
    ├── StepDefinitions/               # 步驟定義
    └── Support/                       # 測試輔助類別
```
