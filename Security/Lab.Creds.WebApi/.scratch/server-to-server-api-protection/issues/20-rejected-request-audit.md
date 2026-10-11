# 20: 被拒絕申請嘗試的稽核

**What to build:** 申請端點上被拒絕的嘗試也留下稽核紀錄：身分未驗證、持有證明失敗、超過待核准上限、被撤銷或停用的 Client 提出申請。紀錄標為「未驗證」，不把請求宣稱的 Client 身分記成已驗證，內容不含私鑰與原始 Token。失敗回應不洩漏他人申請內容。這些紀錄沿用管理操作稽核的保存與存取規則，與呼叫者安全稽核分開管理。

**Blocked by:** 17 — 憑證登錄申請的申請者驗證與持有證明；18 — 簽章金鑰登錄申請的申請者驗證與持有證明；19 — 每個 Client 的待核准申請數上限；15 — 管理操作稽核。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；稽核欄位與被拒絕嘗試的範圍為 lab 暫定值，待使用者確認；全新 Client 首次登錄不在本單範圍；本單無 Gateway 驗收項目）

- [x] 身分未驗證、持有證明失敗、超過上限、被撤銷或停用 Client 提出的申請，都產生標為「未驗證」的紀錄（AC-30）。
- [x] 紀錄不含私鑰與原始 Token，宣稱的 Client 身分不被記成已驗證。
- [x] 失敗回應不洩漏其他 Client 的申請內容（AC-30）。
- [x] 稽核寫入失敗時，申請不建立並明確回報服務錯誤，與既有管理操作稽核行為一致。
- [x] 每個驗收項目至少對應一個 BDD Scenario（行為 Scenario 未先見紅燈，見下方實作紀錄的更正）。

## 實作紀錄（20）

**範圍與暫定值（lab 暫定、待使用者確認）**：
- 被拒絕嘗試的紀錄沿用 15 單的管理操作稽核（程序內、保存期 90 天、僅追加、只經管理員查詢端點讀取）；稽核欄位與保存規則為 lab 暫定值，待使用者確認。
- 「未驗證」以 `actorStatus` 表示：只有已驗證管理員才標為「已驗證管理員」，其餘申請嘗試一律為「未驗證」。`actorThumbprint` 只是出示的憑證指紋，不視為身分。
- 紀錄的 `clientId` 欄位記錄申請宣稱的 Client（對象），它不代表已驗證身分；區分依據是 `actorStatus`。這是 lab 暫定的記錄方式，待使用者確認。

**行為**：
- 15、17、19 單的決策閘門已讓每一次申請決策（含 401、400、403、429）在受理前寫入稽核，並標為 `rejected`；本單沒有改動正式程式，只以 BDD 把上述行為鎖定為驗收。
- 稽核寫入失敗時回 `503 audit_unavailable`，申請不建立；之後連續申請數仍可達上限，證明失敗的申請沒有佔用額度。
- 紀錄不含私鑰、原始 Token 或 PEM 內容；失敗回應只含 `error` 欄位，不含他人申請的識別或指紋。

**測試**：
- 新增 `AuthSpike.Tests/Features/RejectedRequestAudit.feature` 與 `Steps/RejectedRequestAuditSteps.cs`，所有申請與稽核讀取皆經真實 mTLS 與 HTTP 端點，不直接呼叫內部方法。
- 更正（審查意見）：原紀錄宣稱先紅後綠的 TDD 順序，但行為 Scenario 未先見紅燈，它們加入時即在現有程式下通過，未符合先紅後綠的流程。實際行為由 15、17、19 單的實作覆蓋；本單的 Scenario 是驗收鎖定，不是實作驅動。`@record` Scenario 因實作紀錄尚未存在而失敗，補上本紀錄後轉綠。
- 測試接線過程中，稽核寫入失敗情境的一個步驟因 Reqnroll 依關鍵字類型比對而未對上，已調整為 `When` 步驟；這是測試接線問題，未改動產品行為。
- 既有測試斷言未放寬。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```
（Feature 名稱為 `RejectedRequestAudit`，Scenario 列於 `AuthSpike.Tests/Features/RejectedRequestAudit.feature`。）

**範圍限制**：全新 Client 首次登錄不在本單；稽核與申請仍為程序內儲存，只在單程序內驗證；Gateway 相關項目略過（非完成）。
