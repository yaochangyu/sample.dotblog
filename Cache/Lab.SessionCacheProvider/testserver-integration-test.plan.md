# TestServer 整合測試計畫

使用 `Microsoft.AspNetCore.TestHost` 的 TestServer，驗證 ASP.NET Core 環境下 SessionCacheProvider 的端對端行為。

## 步驟

- [x] **1. 新增 Feature 檔案 `TestServerIntegration.feature`**
  - 驗證真實 HTTP 請求下的行為：首次請求自動建立 SessionCacheId cookie、帶 cookie 回傳可取回先前設定的值、不同 session 互不干擾
  - 為什麼：目前的 BDD 測試使用 NSubstitute mock HttpContext，無法驗證真實 Cookie 寫入/讀取與 DI 管線是否正確串接

- [x] **2. 建立 TestServer 的 Host 設定（WebApplicationFactory 或直接 TestServer）**
  - 在測試專案新增一個最小的 API 應用程式，註冊 `AddHybridCache()` + `AddSessionCacheProvider()` + `UseSessionCache()`，並提供測試用的 Endpoint
  - 為什麼：TestServer 需要一個真實的 ASP.NET Core 管線才能驗證中介軟體、DI、Cookie 的完整流程

- [x] **3. 新增 StepDefinitions `TestServerIntegrationStepDefinitions.cs`**
  - 使用 HttpClient 發送 HTTP 請求，驗證 response cookie 與回傳值
  - 為什麼：BDD 步驟需要對應的實作來驅動 TestServer

- [x] **4. 更新專案檔 `Lab.SessionCacheProvider.Tests.csproj`**（加入 Microsoft.AspNetCore.TestHost 套件）
  - 確認是否需要額外的套件參考（TestServer 已包含在 `Microsoft.AspNetCore.App` FrameworkReference 中）
  - 為什麼：確保測試專案有所有必要的依賴

- [x] **5. 建置與執行測試**
  - `dotnet build` + `dotnet test` 確認所有測試通過
  - 為什麼：驗證新增的整合測試與既有單元測試都能正常運作

- [x] **6. 更新 `@tree.md`**
  - 反映新增的檔案
  - 為什麼：維護專案結構文件的一致性
