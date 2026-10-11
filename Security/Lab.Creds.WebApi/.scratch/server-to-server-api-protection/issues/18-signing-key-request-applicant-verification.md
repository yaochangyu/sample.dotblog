# 18: 簽章金鑰登錄申請的申請者驗證與持有證明

**What to build:** 簽章金鑰登錄申請端點套用同一套申請者驗證：只有已登錄且狀態有效的 Client 能以現有憑證經 mTLS 驗證身分，並只能為自己申請。持有證明改為以所申請的新金鑰對申請內容簽章，證明失敗不建立待核准紀錄。待核准紀錄帶已驗證的申請者身分。與憑證申請各自獨立，兩者的驗證結果互不連動。

**Blocked by:** 17 — 憑證登錄申請的申請者驗證與持有證明；13 — 簽章金鑰登錄申請與核准。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；簽章內容與申請欄位為 lab 暫定值，待使用者確認；全新 Client 首次登錄不在本單範圍；本單無 Gateway 驗收項目）

- [x] 未經驗證送出簽章金鑰申請時被拒絕，不建立待核准紀錄（AC-24）。
- [x] 以自己的身分替其他 Client 提出簽章金鑰申請時被拒絕（AC-25）。
- [x] 申請簽章金鑰但無法以該金鑰對申請內容簽章時被拒絕，不建立待核准紀錄（AC-26）；簽章涵蓋範圍為 lab 暫定值並標註「待使用者確認」。
- [x] 已登錄 Client 為自己申請並證明持有新金鑰時，建立待核准紀錄且帶已驗證的申請者身分（AC-27 簽章金鑰部分）。
- [x] 被撤銷、停用或已退役憑證的 Client 提出申請時被拒絕（AC-29 簽章金鑰部分）。
- [x] 失敗回應不洩漏其他 Client 的申請內容。
- [x] 簽章金鑰申請與憑證申請的驗證互不連動：憑證申請通過不代表簽章金鑰申請通過。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作；沿用既有簽章規則與管理端點整合測試邊界。

## 實作紀錄（18）

**範圍與端點（lab 暫定、待使用者確認）**：
- 提交簽章金鑰申請：`POST /signing-key-requests`，申請者驗證與持有證明與 17 單的憑證申請共用同一決策路徑（`DecideSigningKeySubmission` 與 `AuthenticateApplicant`），並由 OpenAPI 契約（`poc/AuthSpike/doc/openapi.yml`）描述。
- 持有證明（lab 暫定、待使用者確認）：以申請中金鑰的私鑰（ECDSA P-256 SHA-256，IEEE P1363）對 `signing-key-request\n{clientId}\n{keyId}\n{公開金鑰 SPKI SHA-256}` 簽章，內容由伺服器依申請欄位重建。
- 申請者身分：出示的 mTLS 憑證必須是某 Client 目前登錄且未撤銷、未退役的已核准憑證，且該 Client 未停用；待核准憑證不是申請者身分。

**行為**：
- 未驗證（無憑證、未登錄憑證、待核准憑證、已撤銷、已退役、Client 已停用）一律回 401，不區分 Client 是否存在（AC-24、失敗回應不洩漏）。
- 已驗證申請者替其他 Client 申請回 403，回應不含對方的申請識別或 keyId（AC-25）。
- 持有證明缺漏或由他人私鑰產生回 400，不建立待核准紀錄（AC-26）。
- 通過者建立待核准紀錄並帶已驗證的申請者身分，管理員查詢回應可見（AC-27 簽章金鑰部分）。
- 憑證申請與簽章金鑰申請各自獨立驗證：憑證申請待核准不影響簽章金鑰申請通過；憑證申請通過不代表簽章金鑰申請通過（第 7 項）。

**紅綠流程（誠實紀錄）**：`SigningKeyApplicantVerification.feature` 與步驟先加入後執行全套測試。行為斷言在首次執行即已通過，因為 17 單已把申請者驗證與持有證明加入共用的簽章金鑰決策路徑，本單未另行修改產品程式碼。首次失敗的 14 個 Scenario 都停在「18 單第 N 項驗收已勾選」與「實作紀錄包含」的紀錄斷言，待勾選與記錄完成後轉綠。因此本單主要新增的是簽章金鑰路徑的行為證據，而非新的產品行為。

**測試邊界**：測試經真實 mTLS 與 HTTP 端點，不直接呼叫內部登錄方法；共用 17 單的審計計數輔助（`ApplicantVerificationSteps.AcceptedSubmissionCountAsync`，由 private 改為 internal）。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```
（Feature 名稱為 `SigningKeyApplicantVerification`，Scenario 列於 `AuthSpike.Tests/Features/SigningKeyApplicantVerification.feature`。）

**範圍限制**：同一 Client 待核准上限（AC-28）屬 19 單；被拒絕嘗試的標示紀錄（AC-30）屬 20 單；全新 Client 首次登錄與其申請管道不在本單；申請仍為程序內儲存，只在單程序內驗證；Gateway 相關項目略過（非完成）。
