# 移除 HttpContext.Current 重構計畫

## 目標
移除 `HttpContext.Current` 靜態依賴，改用 `ICookieAccessor` 抽象介面統一兩個平台的 Cookie 存取。

## 設計

```
ICookieAccessor（共用介面）
├── GetSessionId(): string?
├── SetSessionId(string sessionId): void
│
├── AspNetCookieAccessor（.NET Framework，注入 HttpContextBase）
└── AspNetCoreCookieAccessor（.NET 10，注入 IHttpContextAccessor）

SessionCacheProvider（不再有條件編譯）
├── 建構子注入 HybridCache + ICookieAccessor
└── Session 屬性回傳 SessionObject
```

## 步驟

- [x] **步驟 1：建立 ICookieAccessor 介面**
  - 定義 `GetSessionId()` 與 `SetSessionId()` 方法
  - 共用，無條件編譯
  - **原因**：抽象 Cookie 存取，解除平台耦合

- [x] **步驟 2：實作 AspNetCookieAccessor（.NET Framework）**
  - 透過建構子注入 `HttpContextBase`，不再使用 `HttpContext.Current`
  - 用 `#if NETFRAMEWORK` 條件編譯
  - **原因**：.NET Framework 平台的 Cookie 存取實作

- [x] **步驟 3：實作 AspNetCoreCookieAccessor（.NET 10）**
  - 透過建構子注入 `IHttpContextAccessor`
  - 用 `#if !NETFRAMEWORK` 條件編譯
  - **原因**：.NET Core/10 平台的 Cookie 存取實作

- [ ] **步驟 4：重構 SessionCacheProvider**
  - 移除所有條件編譯與 `HttpContext.Current`
  - 改為實例類別，建構子注入 `HybridCache` + `ICookieAccessor`
  - **原因**：核心類別不再依賴平台特定 API

- [ ] **步驟 5：更新 SessionCacheProviderExtensions**
  - .NET 10 的 `AddSessionCacheProvider` 註冊 `AspNetCoreCookieAccessor`
  - **原因**：DI 註冊需對應新的抽象

- [ ] **步驟 6：更新 BDD 測試**
  - 測試改用 `ICookieAccessor` Mock 或 Fake
  - 確保 9 個測試全數通過
  - **原因**：配合重構調整測試

- [ ] **步驟 7：Build + 測試驗證**
  - `dotnet build` 兩個目標框架編譯通過
  - `dotnet test` 全數通過
  - **原因**：確保重構未破壞功能
