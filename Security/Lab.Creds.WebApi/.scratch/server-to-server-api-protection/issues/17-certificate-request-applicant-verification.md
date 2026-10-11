# 17: 憑證登錄申請的申請者驗證與持有證明

**What to build:** 憑證登錄申請端點不再接受匿名申請。只有已登錄且狀態有效的 Client，能以現有已核准的憑證經 mTLS 驗證身分後，為自己提出申請；宣稱的 Client 必須與已驗證的身分一致，不能替其他 Client 申請。申請者還必須證明持有所申請新憑證的私鑰，證明失敗的申請不建立待核准紀錄。待核准紀錄帶有已驗證的申請者身分，讓管理員核准時可以核對。被撤銷、停用或已退役憑證的 Client 不能申請。失敗回應不洩漏其他 Client 的申請內容，也不讓未驗證者區分「Client 不存在」與「身分驗證失敗」。

**Blocked by:** 11 — 管理介面與管理員身分驗證；12 — 憑證登錄申請與核准／拒絕。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；持有證明的表示方式與申請欄位為 lab 暫定值，待使用者確認；全新 Client 首次登錄不在本單範圍；本單無 Gateway 驗收項目）

- [x] 未經驗證（無憑證或未登錄憑證）送出憑證登錄申請時被拒絕，不建立待核准紀錄（AC-24）。
- [x] 已登錄 Client 以自己的身分替其他 Client 提出申請時被拒絕，對方的待核准清單不變（AC-25）。
- [x] 申請新憑證但無法證明持有對應私鑰時被拒絕，不建立待核准紀錄（AC-26）；持有證明的具體表示方式為 lab 暫定值並標註「待使用者確認」。
- [x] 已登錄 Client 以現有憑證為自己申請並證明持有新私鑰時，建立待核准紀錄且帶已驗證的申請者身分（AC-27 憑證部分）。
- [x] 被撤銷、停用或已退役憑證的 Client 提出申請時被拒絕（AC-29 憑證部分）。
- [x] 失敗回應不洩漏其他 Client 的申請內容，且未驗證者無法區分「Client 不存在」與「身分驗證失敗」。
- [x] 全新 Client 的首次登錄不在本票範圍，不自行假設其申請管道。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作；沿用管理端點為主要整合測試邊界，不新增測試介面。

## 實作紀錄（17）

**範圍與端點（lab 暫定、待使用者確認）**：
- 提交憑證申請：`POST /client-certificate-requests`，本體新增 `possessionProof`（lab 暫定、待使用者確認）；申請者以出示的 mTLS 憑證判定，必須是某 Client 目前登錄且未撤銷、未退役的憑證，且該 Client 未停用。
- 持有證明（lab 暫定、待使用者確認）：憑證申請以新憑證的私鑰（RSA PKCS#1 v1.5 SHA-256）對 `client-certificate-request\n{clientId}\n{thumbprint}` 簽章；簽章金鑰申請以該金鑰（ECDSA P-256 SHA-256）對 `signing-key-request\n{clientId}\n{keyId}\n{公開金鑰 SPKI SHA-256}` 簽章。內容由伺服器依申請欄位重建。
- 管理員查詢：`GET /admin/client-certificate-requests/{requestId}` 與簽章金鑰對應端點新增 `applicantClientId`、`applicantThumbprint`，供核准時核對申請者身分。

**行為**：
- 未驗證（無憑證、未登錄、已撤銷或已退役憑證、Client 已停用）一律回 401 `applicant_authentication_required`，不區分 Client 是否存在（AC-24、失敗回應不洩漏）。
- 已驗證申請者替其他 Client 申請回 403 `applicant_mismatch`，不回傳對方的申請內容（AC-25）。
- 持有證明缺漏或無效回 400 `possession_proof_invalid`；證明在受理前驗證，失敗不建立待核准紀錄（AC-26）。
- 通過者建立待核准紀錄並帶已驗證的申請者身分（AC-27 憑證部分）；撤銷、停用、退役後的同一憑證再申請回 401（AC-29 憑證部分）。
- 申請仍只記錄於程序內 `CertificateRegistrationRequests` / `SigningKeyRegistrationRequests`；申請不改變信任名單，也不能取得 Token。

**既有測試的調整**：12、13、14、15、16 單的測試原本以匿名方式提交申請，本單後端點必須經 mTLS 驗證並附持有證明，因此這些測試改以申請者本人的已登錄憑證送出並附證明；斷言與預期結果未放寬。`orders-partner-client` 與 `orders-client` 的重複指紋情境（AC-23）以 `orders-client` 憑證的私鑰產生證明，只驗證核准時的重複檢查，不代表真實場景。

**稽核**：申請仍寫入管理操作稽核（既有行為），未驗證與證明失敗的嘗試以 `rejected` 與 `actorStatus` 記錄；「標為未驗證的紀錄」的完整要求屬 20 單。

**紅綠流程**：先加入 `ApplicantVerification.feature` 與步驟，編譯後行為 Scenario 因端點仍接受匿名申請而失敗；實作後行為 Scenario 轉綠，之後才勾選本單驗收項目。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```
（Feature 名稱為 `ApplicantVerification`，Scenario 列於 `AuthSpike.Tests/Features/ApplicantVerification.feature`。）

**範圍限制**：同一 Client 待核准上限與逾期處理（AC-28）屬 19 單；被拒絕嘗試的標示紀錄（AC-30）屬 20 單；全新 Client 首次登錄與其申請管道不在本單；申請仍為程序內儲存，只在單程序內驗證；Gateway 相關項目略過（非完成）。
