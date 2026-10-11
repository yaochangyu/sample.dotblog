# 19: 每個 Client 的待核准申請數上限

**What to build:** 每個 Client 的待核准申請（憑證申請與簽章金鑰申請合計）有上限。達到上限後，該 Client 的新申請被明確拒絕，既有待核准申請不受影響，其他 Client 的申請額度也不受影響。管理員核准或拒絕後，額度釋放，Client 可再提出新申請。

**Blocked by:** 17 — 憑證登錄申請的申請者驗證與持有證明；18 — 簽章金鑰登錄申請的申請者驗證與持有證明。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；上限數值 5 與逾期處理為 lab 暫定值，待使用者確認；本單無 Gateway 驗收項目）

- [x] 同一 Client 的待核准申請達到上限後，新申請被明確拒絕，既有待核准申請不受影響（AC-28）。
- [x] 上限對憑證與簽章金鑰申請合計計算，不會因分開提出而繞過。
- [x] 一個 Client 達到上限不影響其他 Client 提出申請。
- [x] 管理員核准或拒絕後額度釋放，Client 可再提出新申請。
- [x] 上限數值與逾期處理為 lab 暫定值並標註「待使用者確認」，不自行視為已核准。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作。

## 實作紀錄（19）

**範圍與數值（lab 暫定、待使用者確認）**：
- 每個 Client 的待核准申請上限為 5 筆（`AuthServerHost.PendingRequestLimitPerClient`），憑證申請與簽章金鑰申請合計計算。
- 逾期處理（lab 暫定、待使用者確認）：本 lab 不設自動逾期，待核准申請只在管理員核准或拒絕後離開待核准狀態，因此額度只因此釋放；正式採用前須決定有效期與逾期規則。
- 超過上限的回應：`429 Too Many Requests`，錯誤為 `pending_request_limit_reached`（lab 暫定、待使用者確認）；OpenAPI 契約 `poc/AuthSpike/doc/openapi.yml` 兩個申請端點已補上 429。

**行為**：
- 上限判斷在申請者驗證與持有證明之後、受理之前，並在決策閘門內執行，因此併發申請不會突破上限（AC-28）。
- 計數只計 `Pending` 狀態：`CertificateRegistrationRequests.CountPending` 與 `SigningKeyRegistrationRequests.CountPending` 依 Client 計數，兩者相加判斷。
- 超過上限的嘗試不建立待核准紀錄，既有待核准申請狀態不變；其他 Client 的計數互不影響。
- 核准或拒絕後該申請不再計為待核准，額度釋放，同一 Client 可再提出新申請。
- 超過上限的嘗試仍寫入管理操作稽核（`outcome` 為 `rejected`，`reason` 為 `pending_request_limit_reached`）；被拒絕嘗試標為未驗證的完整紀錄屬 20 單。

**測試**：
- 新增 `AuthSpike.Tests/Features/PendingRequestCap.feature` 與 `Steps/PendingRequestCapSteps.cs`，所有呼叫經真實 mTLS 與 HTTP 管理端點，不直接呼叫內部登錄方法。
- 先加入 Scenario 並執行：行為 Scenario 失敗於「預期 429、實際 202」，符合紅燈；實作後行為 Scenario 轉綠，之後才勾選本單驗收項目與紀錄 Scenario。
- 預設執行環境在所有情境間共用，先前情境未核准的申請會佔用額度。因此新增 `Support/PendingRequestCleanup.cs` 與 `SpikeHooks` 的 `AfterScenario`：情境結束時把本情境於目前執行環境受理的申請以管理員拒絕（只改狀態，不改信任名單），讓後續情境從零待核准開始。上限情境標示 `@isolated`，使用獨立執行環境。
- 既有測試的斷言未放寬。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```
（Feature 名稱為 `PendingRequestCap`，Scenario 列於 `AuthSpike.Tests/Features/PendingRequestCap.feature`。）

**範圍限制**：全新 Client 首次登錄不在本單；被拒絕嘗試的標示紀錄（AC-30）屬 20 單；申請仍為程序內儲存，只在單程序內驗證；Gateway 相關項目略過（非完成）。
