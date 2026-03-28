# SessionCacheProvider 實作計畫

## 目標
用 HybridCache + Cookie 取代 ASP.NET Session，解決 Session 互斥鎖導致 request 排隊的問題。
輸出為 Class Library，支援 .NET Framework 及 .NET 10。

## 設計概要
參考 darkthread 部落格的 UnobtrusiveSession 概念：
- **Cookie**：儲存 SessionId（GUID），用於識別工作階段
- **HybridCache**：取代 MemoryCache 儲存實際資料，支援 L1/L2 快取
- **鎖定範圍**：僅在資料讀寫時短暫鎖定，避免整個 request 被序列化

## 使用方式對照

```csharp
// 原本 ASP.NET Session
Session["UserName"] = "John";
var name = Session["UserName"];

// SessionCacheProvider（.NET Framework 靜態存取）
SessionCacheProvider.Session["UserName"] = "John";
var name = SessionCacheProvider.Session["UserName"];

// SessionCacheProvider（.NET 10 透過 DI 注入）
_sessionCacheProvider.Session["UserName"] = "John";
var name = _sessionCacheProvider.Session["UserName"];
```

## 核心類別設計

```
SessionObject - 內部類別，提供 indexer this[string key] 存取資料
├── 內部持有 HybridCache 實例與 SessionId
├── this[key] get → HybridCache.GetOrCreateAsync (同步包裝)
├── this[key] set → HybridCache.SetAsync (同步包裝)
└── Remove(key) → HybridCache.RemoveAsync

SessionCacheProvider - 主要類別
├── .NET Framework：static Session 屬性，透過 HttpContext.Current 取得 Cookie
├── .NET 10：實例 Session 屬性，透過 IHttpContextAccessor 取得 Cookie
└── Cookie 管理：讀取/建立 SessionId
```

## 專案結構
```
Lab.SessionCacheProvider/
├── Lab.SessionCacheProvider.sln
├── @tree.md
├── session-cache-provider.plan.md
└── Lab.SessionCacheProvider/
    ├── Lab.SessionCacheProvider.csproj   (multi-target: net48;net10.0)
    ├── SessionObject.cs                   (索引器類別，包裝 HybridCache 操作)
    ├── SessionCacheProvider.cs            (核心實作，管理 Cookie 與 Session 屬性)
    └── SessionCacheProviderExtensions.cs  (DI 註冊擴充方法，僅 .NET 10)
```

## 步驟

- [x] **步驟 1：建立方案檔與專案檔**
  - 建立 `Lab.SessionCacheProvider.sln` 與 `Lab.SessionCacheProvider.csproj`
  - 多目標框架：`net48;net10.0`
  - 加入必要 NuGet 套件：
    - 共用：`Microsoft.Extensions.Caching.Hybrid`
    - .NET Framework：`System.Web`（存取 HttpContext.Current / Cookie）
    - .NET 10：`Microsoft.AspNetCore.Http`（IHttpContextAccessor）
  - **原因**：需要方案與專案基礎結構才能開始開發

- [x] **步驟 2：實作 SessionObject**
  - 提供 `this[string key]` 索引器，get/set 對應 HybridCache 操作
  - 提供 `Remove(string key)` 方法
  - HybridCache key 格式：`session:{sessionId}:{key}`
  - 滑動過期預設 20 分鐘
  - **原因**：這是貼近 `Session["key"]` 使用習慣的關鍵設計

- [x] **步驟 3：實作 SessionCacheProvider**
  - Cookie 管理：讀取/建立 SessionId（GUID）存入 Cookie
  - `Session` 屬性回傳 `SessionObject` 實例
  - .NET Framework：`static` 屬性，透過 `HttpContext.Current` 存取 Cookie
  - .NET 10：實例屬性，透過 `IHttpContextAccessor` 存取 Cookie
  - 使用條件編譯 `#if NETFRAMEWORK` / `#else` 區分平台
  - **原因**：核心入口，串接 Cookie 識別與 HybridCache 資料存取

- [x] **步驟 4：實作 DI 擴充方法（.NET 10）**
  - `AddSessionCacheProvider` 擴充方法，註冊相關服務到 DI 容器
  - 僅在 .NET 10 目標下編譯（`#if !NETFRAMEWORK`）
  - **原因**：讓 .NET 10 專案能透過標準 DI 方式註冊使用

- [x] **步驟 5：Build 驗證**
  - 執行 `dotnet build` 確保兩個目標框架都能編譯通過
  - **原因**：確保多目標框架的相容性沒有問題
