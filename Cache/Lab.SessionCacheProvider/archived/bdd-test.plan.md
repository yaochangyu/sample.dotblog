# BDD 測試專案實作計畫

## 目標
為 SessionCacheProvider 撰寫 BDD 測試案例，使用 Reqnroll + xUnit。

## 測試範圍
1. **SessionObject** — 索引器存取、型別化存取、移除
2. **SessionCacheProvider** — Cookie 管理（新建/重用 SessionId）、Session 屬性回傳 SessionObject

## 專案結構
```
Lab.SessionCacheProvider.Tests/
├── Lab.SessionCacheProvider.Tests.csproj  (net10.0, Reqnroll + xUnit)
├── Features/
│   ├── SessionObject.feature
│   └── SessionCacheProvider.feature
├── StepDefinitions/
│   ├── SessionObjectStepDefinitions.cs
│   └── SessionCacheProviderStepDefinitions.cs
└── Support/
    └── TestDependencies.cs  (ScenarioDependencies DI 設定，註冊 Mock)
```

## 步驟

- [x] **步驟 1：建立測試專案與相依套件**
  - 建立 `Lab.SessionCacheProvider.Tests.csproj`，目標 `net10.0`
  - 加入 Reqnroll.xUnit、xUnit、NSubstitute、Microsoft.NET.Test.Sdk
  - 加入專案參考 Lab.SessionCacheProvider
  - 加入到方案檔
  - **原因**：測試專案基礎結構

- [x] **步驟 2：撰寫 SessionObject.feature**
  - 場景：透過索引器設定與取得值
  - 場景：設定 null 值等同移除
  - 場景：透過 Remove 方法移除值
  - 場景：取得不存在的 key 回傳 null
  - **原因**：驗證 SessionObject 索引器行為符合 Session 使用習慣

- [x] **步驟 3：撰寫 SessionObjectStepDefinitions.cs**
  - 實作步驟 2 Feature 對應的 Step Definitions
  - 使用真實 HybridCache（透過 AddHybridCache 註冊 in-memory）
  - **原因**：Feature 需要對應的步驟實作才能執行

- [x] **步驟 4：撰寫 SessionCacheProvider.feature**
  - 場景：無 Cookie 時自動建立新的 SessionId
  - 場景：有 Cookie 時重用既有 SessionId
  - 場景：Session 屬性回傳可操作的 SessionObject
  - **原因**：驗證 Cookie 管理與 Session 屬性的整合行為

- [x] **步驟 5：撰寫 SessionCacheProviderStepDefinitions.cs + Support**
  - Mock IHttpContextAccessor 模擬 Cookie 讀寫
  - 實作步驟 4 Feature 對應的 Step Definitions
  - **原因**：需要 Mock HttpContext 才能測試 Cookie 管理邏輯

- [x] **步驟 6：執行測試並驗證通過**
  - 執行 `dotnet test` 確保所有測試通過
  - **原因**：確保實作正確

