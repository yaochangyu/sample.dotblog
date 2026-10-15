# 03: 呼叫者類型與 audience 隔離（禁止混用）

**What to build:** 服務 Token 與使用者 Token 互不通用。使用者持服務 Token 走使用者驗證、服務持使用者 Token 走服務驗證、或 Token 的 audience 與呼叫者類型不符，一律被拒絕。呼叫者類型由已驗證的 Token 決定，不由請求內容自行宣稱。任一路徑都不能降級另一路徑：使用者驗證較弱，也不能讓服務呼叫跟著走弱的那條。

**Blocked by:** 02 — 使用者 Token 簽發與使用者驗證路徑。

**Status:** resolved

- [x] 使用者持服務 Token 走使用者驗證時被拒絕（AC-D03）。
- [x] 服務持使用者 Token 走服務驗證時被拒絕（AC-D04）。
- [x] Token 的 audience 或呼叫者類型不符時被拒絕（AC-D05）。
- [x] 請求自行宣稱的呼叫者類型不被採信；驗證失敗者只標為未驗證。
- [x] 服務路徑仍強制 mTLS 綁定、請求簽章與 nonce，不因使用者路徑存在而放寬。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作；沿用既有整合測試邊界。

## 實作紀錄

### 變更內容

- `poc/AuthSpike.Tests/Features/CallerTypeAndAudienceIsolation.feature`（新增）：9 個行為 Scenario（`@caller-isolation @isolated`）與 1 個 `@record` 情境，逐項對應 AC-D02、AC-D03、AC-D04、AC-D05，並涵蓋宣稱身分不採信與驗證失敗只標為未驗證。
- `poc/AuthSpike.Tests/Steps/CallerTypeAndAudienceIsolationSteps.cs`（新增）：自成一組步驟（真實 client_credentials、password grant、內省與建立訂單請求），步驟文字以「03 單」區分，不共用其他 Steps 類別的狀態。
- `poc/AuthSpike/`（產品程式碼）本票無變更。

### 差距分析（實作前）

- 路由：出示用戶端憑證走服務路徑、未出示走使用者路徑（02 單既有）。服務 Token 在使用者路徑因 audience 不符被內省為 inactive；使用者 Token 在服務路徑因 audience 不符被 OpenIddict 驗證拒絕。兩方向的隔離主要由授權伺服器的 audience 綁定達成，閘門上的 audience 與 caller_type 檢查為第二道防線。
- 服務路徑未見降級：出示憑證即只走服務路徑，不回退使用者路徑。
- 因此 03 單以行為覆蓋為主，未修改產品程式碼。

### TDD 順序與結果

- 紅燈：先寫 Feature（9 個行為 Scenario 與 `@record`），執行篩選後 10 個情境失敗，全數因步驟未定義（undefined）；既有 02 單等情境維持通過。
- 綠燈（行為）：補上步驟後，行為 Scenario 全數通過，屬於特徵化覆蓋（既有保護已生效，不是行為變更）。
- `@record`：在本票勾選完成前為紅燈，勾選與狀態改為 resolved 後轉綠。

### 觀察（未修正，記錄待後續）

- `UserTokenIntrospector` 對內省回應缺少 `caller_type`、`client_id` 或 `sub` 時以 `Required` 拋例外，經外層 catch 回報 `Unavailable`（503）。此情況只在授權伺服器回傳 active 但缺少這些 claim 時才會發生，目前授權伺服器對使用者 Token 必定附帶這些 claim，實際上無法由測試觸發，因此未修改。若日後授權伺服器的 claim 變動，應改為明確的拒絕。
- 「audience 正確、呼叫者類型錯誤」的 Token 在目前的授權伺服器無法產生（使用者 audience 只會配發給使用者 Client），因此 AC-D05 的呼叫者類型不符只能以服務 Token 走使用者路徑（audience 與類型同時不符）覆蓋；單獨的類型不符無法在真實協定下重現。

### lab 暫定、待使用者確認

- 呼叫者類型以出示用戶端憑證與否決定路由（02 單既有暫定值，待使用者確認）。
- 使用者 Token 的 audience 與 caller_type 表示方式（`orders-api-user`、`caller_type=user`）沿用 02 單，待使用者確認。

### 驗證方式（可重現）

- `dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
- 只跑本票：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "DisplayName~AC-D0|DisplayName~請求宣稱|DisplayName~驗證失敗的請求|DisplayName~03 單"`
- 完整：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj`，結果 376 通過、0 失敗、2 略過（既有 `@ignore` 情境）。

### 修正紀錄

- 首次完整測試有 22 個失敗，原因是本票的驗收勾選步驟文字「03 單第 N 項驗收已勾選」「03 單的狀態為 resolved」與既有 server-to-server 03 單的步驟重複，Reqnroll 判定為歧義（Ambiguous）。已將本票的兩個步驟改為「呼叫者隔離票」前綴，之後完整測試全數通過。
