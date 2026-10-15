# 01: 統一的已驗證呼叫者與呼叫者類型（前置重構）

**What to build:** 把現有服務呼叫路徑的驗證結果，改為產出統一的「已驗證呼叫者」：包含呼叫者類型（目前只有服務）、已驗證身分與權限。業務授權與業務邏輯只依這個已驗證呼叫者判斷，不再直接依服務驗證的內部細節。這是不改行為的前置重構，為後續加入使用者路徑鋪路；現有的服務呼叫行為與全部測試維持不變。

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] 服務呼叫路徑驗證成功後，產出包含呼叫者類型（服務）、已驗證身分與權限的已驗證呼叫者；授權與業務處理改為只依它判斷（AC-D02）。
- [x] 服務呼叫的接受與拒絕行為與現有完全一致：mTLS 綁定、請求簽章、nonce、撤銷、fail closed、冪等與稽核不變；現有完整測試套件 0 失敗、不得刪弱既有斷言。
- [x] 驗證失敗時，請求宣稱的呼叫者類型與身分只能標為未驗證，不產出已驗證呼叫者。
- [x] 新增一個回歸 Scenario，證明服務呼叫在重構後仍受完整保護（AC-D02）。
- [x] 每個驗收項目至少對應一個 BDD Scenario（Feature、Scenario、Given、When、Then 用英文，步驟用中文）；沿用既有主要整合測試邊界，不新增測試介面。
- [x] 詞彙使用 `GLOSSARY.md` 既有用語；新增「呼叫者類型」草稿定義，待 08 單統一補入詞彙表。

## 實作紀錄

### 變更內容

- `poc/AuthSpike/Verification/VerifiedCaller.cs`（新增）：`CallerType`（目前只有 `Service`）與 `VerifiedCaller`（呼叫者類型、已驗證 Client 身分、權限 scope）。以 `HttpContext.Items` 附加／撤銷，不改變公開端點。
- `poc/AuthSpike/OrdersApi/OrdersApiHost.cs`：
  - 查證閘門在 Token 查證成功時附加已驗證的服務呼叫者；查證失敗、無 Token、管理員憑證等路徑不附加，宣稱的身分仍只進入未驗證欄位。
  - 簽章與防重放未通過時撤銷已附加的呼叫者；只有簽章通過（`Accepted`）才會進入業務處理。
  - 授權規則（`orders.read`／`orders.write`）改依已驗證呼叫者的權限判斷，不再直接讀 `context.User`。
  - 三個業務端點（建立、查詢、取消）改取 `VerifiedCaller.Identity`，不再直接讀 Token 的 `client_id` claim。
  - 行為不變：401／403／503 的判斷順序、稽核寫入點與紀錄內容皆未改。
- `GLOSSARY.md`：新增「呼叫者類型」草稿定義，標註待 08 單統一補入。
- `poc/AuthSpike.Tests/Features/UnifiedAuthenticatedCaller.feature`（新增）：7 個 Scenario（`@unified-caller @isolated`）與 1 個 `@record` 情境，涵蓋 AC-D02 的服務保護回歸與驗證失敗僅標為未驗證；步驟沿用既有稽核步驟。
- `poc/AuthSpike.Tests/Steps/UnifiedAuthenticatedCallerSteps.cs`（新增）：只含 `@record` 情境所需的勾選與狀態檢查步驟。

### TDD 順序與結果

- 紅燈：先寫 Feature 後，`@record` 情境因本票勾選尚未完成而失敗（`01 單第 1 項驗收已勾選` 等）；行為情境在重構前即通過，證明保護基線（本票為不改行為的重構，行為情境為特徵化測試，沒有行為紅燈）。
- 綠燈：重構後行為情境全數通過；完整測試套件的唯一失敗為勾選檢查（待本票勾選完成後轉綠）。

### lab 暫定、待使用者確認

- 已驗證呼叫者在「Token 查證成功」時即附加（早於簽章驗證），原因是既有行為中 scope 不足回 403 早於簽章失敗回 401，為不改行為而保留此順序；業務處理只在簽章通過後執行。是否改為只在完整驗證後才產出，待使用者確認（若改，需同時調整 403 與 401 的先後）。
- 呼叫者類型目前只有 `Service`；授權規則依類型分開撰寫留待使用者呼叫者票。

### 驗證方式（可重現）

- `dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
- `dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj`（完整，約 12 分鐘）
- 只跑本票：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "DisplayName~AC-D02|DisplayName~驗證失敗的服務呼叫|DisplayName~01 單驗收項目"`
