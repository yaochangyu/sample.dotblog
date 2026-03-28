# CacheSession 靜態便利類別實作計畫

## 目標
提供靜態類別 `CacheSession`，讓使用方式貼近原生 `Session`：
```csharp
CacheSession.Current["UserName"] = "John";
var name = CacheSession.Current["UserName"];
```

## 設計
- `CacheSession` 靜態類別，提供 `Current` 屬性回傳 `SessionObject`
- .NET Framework：內部用 `HttpContextWrapper(HttpContext.Current)` 建立 `AspNetCookieAccessor`
- .NET 10：內部從 `HttpContext.RequestServices` 解析已註冊的 `SessionCacheProvider`
- 需先呼叫初始化方法設定 `HybridCache` 與 `HybridCacheEntryOptions`

## 步驟

- [x] **步驟 1：實作 CacheSession 靜態類別**
  - .NET Framework：`Initialize(HybridCache, HybridCacheEntryOptions)` 設定靜態欄位，`Current` 用 `HttpContext.Current` 建立 `AspNetCookieAccessor` 取得 SessionObject
  - .NET 10：`Current` 從 `HttpContext.RequestServices` 解析 `SessionCacheProvider`，回傳其 `Session`
  - **原因**：提供開發者最簡潔的靜態存取方式

- [ ] **步驟 2：更新 SessionCacheProviderExtensions**
  - 新增 `UseSessionCache()` middleware 擴充方法（.NET 10），確保服務已註冊
  - **原因**：讓 .NET 10 開發者有明確的啟用入口

- [ ] **步驟 3：新增 BDD 測試**
  - 測試 CacheSession.Current 的 get/set 行為
  - **原因**：驗證靜態便利層功能正確

- [ ] **步驟 4：Build + 測試驗證**
  - `dotnet build` 兩個目標框架編譯通過
  - `dotnet test` 全數通過
  - **原因**：確保新增功能未破壞既有功能

- [ ] **步驟 5：更新 README.md**
  - 加入 CacheSession.Current 的使用說明
  - **原因**：文件同步更新
