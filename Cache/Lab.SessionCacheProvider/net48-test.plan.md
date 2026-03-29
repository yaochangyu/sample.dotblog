# .NET Framework 4.8.1 測試專案計畫

為主專案的 net48 target 補上 BDD 測試，確保 .NET Framework 版本的邏輯正確。

## 步驟

- [x] **1. 將現有測試專案改為 Multi-Target（net48;net10.0）**
  - 為什麼：避免建立第二個測試專案，共用 Feature 檔案與 StepDefinitions，減少維護成本
  - 需要處理：net48 不支援的套件（Mvc.Testing）和 Feature（TestServerIntegration、CacheSession）用條件排除

- [x] **2. 處理 net48 的套件相依性**
  - 為什麼：`Microsoft.AspNetCore.Mvc.Testing`、`FrameworkReference Microsoft.AspNetCore.App` 僅適用 net10.0；net48 需要 `NSubstitute` 和 `System.Web` 參考
  - 用 `Condition` 區分兩個 TFM 的 PackageReference

- [x] **3. 排除 net48 不適用的測試檔案**
  - 為什麼：`TestServerIntegration.feature`、`CacheSession.feature` 及其 StepDefinitions 依賴 ASP.NET Core，net48 無法編譯
  - 用 csproj 的 `Compile Remove` + `None Remove` 條件排除，或用 `#if` 條件編譯

- [x] **4. 建置與執行測試**（net48 建置通過，執行需 Windows；net10.0 全數通過）
  - `dotnet build` 確認 net48 和 net10.0 都能編譯
  - `dotnet test` 確認兩個 TFM 的測試都通過

- [x] **5. 更新 `@tree.md`**
  - 反映 csproj 的變更
