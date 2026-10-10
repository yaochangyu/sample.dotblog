# 13: 簽章金鑰登錄申請與核准

**What to build:** Client 的請求簽章公鑰走同一套申請與核准流程加入信任名單，但與 mTLS 憑證分開管理、分開核准。核准簽章金鑰才能讓該 Client 的請求簽章通過驗證；核准其中一種不會讓另一種生效。申請只含公鑰，不含私鑰。待核准與已拒絕的簽章金鑰不能通過簽章驗證。

**Blocked by:** 12 — 憑證登錄申請與核准／拒絕；03 — 業務 API 驗證原始呼叫端請求簽章。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；簽章金鑰申請端點與欄位為 lab 暫定值，待使用者確認；申請傳遞管道不在本單範圍；本單無 Gateway 驗收項目）

- [x] 簽章金鑰申請在核准前，以該金鑰簽署的請求不能通過簽章驗證（AC-16 簽章金鑰部分）。
- [x] 管理員核准簽章金鑰後，以該金鑰簽署的合法請求可通過驗證（AC-17 簽章金鑰部分）。
- [x] 核准憑證不使簽章金鑰生效，核准簽章金鑰也不使憑證生效，兩者各自要有獨立核准（AC-17 分開核准）。
- [x] 管理員拒絕簽章金鑰申請時不生效，並留下拒絕結果（AC-18）。
- [x] 簽章金鑰只能登錄在申請它的 Client 名下，核准後仍受「金鑰識別必須屬於已驗證 Token 的 Client」限制。
- [x] 申請不得含私鑰；非管理員不能核准。
- [x] 每個驗收項目至少對應一個 BDD Scenario；沿用既有簽章涵蓋範圍與時間窗規則，不改變已核准的簽章規則。

## 實作紀錄（13）

**範圍與端點（lab 暫定、待使用者確認）**：
- 簽章金鑰申請：`POST /signing-key-requests`，本體 `{ clientId, keyId, publicKeyPem }`；公開金鑰為 ECDSA P-256 的 SubjectPublicKeyInfo PEM，`keyId` 即請求簽章 `keyid` 參數所用的識別。回應 202 與 `requestId`、狀態 `pending`；未知 Client 回應 404。
- 管理員查詢：`GET /admin/signing-key-requests/{requestId}`，回應 `keyId` 與狀態（`pending`／`approved`／`rejected`）；只有管理員可查。
- 管理員核准／拒絕：`POST /admin/signing-key-requests/{requestId}/approve`、`.../reject`；非待核准的申請回應 409（`request_not_pending`）。
- 申請內容欄位（lab 暫定、待使用者確認）：只含 `clientId`、`keyId` 與公開金鑰 PEM；含私鑰的內容回應 400（`private_key_not_allowed`），不解析、不儲存。非 P-256 或無法解析的內容回應 400（`invalid_public_key`）。
- 申請傳遞管道未定義，本 lab 端點暫不設驗證；申請本身不能通過簽章驗證。scope 欄位（spec 所列的權限範圍）本單未納入申請內容，待使用者確認是否需要。

**行為**：
- 申請僅記錄於 `SigningKeyRegistrationRequests`（程序內，`lock` 保護）；待核准與已拒絕的金鑰不在 `VerificationKeyStore` 內，業務 API 以既有 `keyid` 查找規則回應 401（AC-16、AC-18）。
- 核准時先把金鑰登錄到申請所屬 Client 名下，成功後才標示已核准（AC-17）；`VerificationKeyStore` 與授權伺服器共用同一個實例，核准立即對業務 API 生效。
- 憑證與簽章金鑰各自獨立的申請、狀態與核准端點；核准其中一種不改變另一種的生效狀態（AC-17 分開核准）。
- 金鑰仍以 `VerificationKeyStore` 依 Client 分組；已驗證 Token 的 Client 之外的金鑰一律找不到（既有規則）。
- 既有的簽章涵蓋範圍、時間窗與防重放規則未改動。組合根啟動時的初始簽章金鑰視為環境基準配置，直接登錄（與初始 mTLS 憑證一致）；新增的金鑰一律經申請與核准。

**待查（未於本單結案）**：
- `SpikeRuntime.RegisterSigningKey` 為 08 單輪替用的組合根方法，仍直接登錄金鑰（不經申請與核准）；它是程序內測試縫，非 HTTP 路徑。正式採用前須改為經核准流程，與 12 單對憑證輪替的處理一致。
- 同一 Client 下若申請相同 `keyId` 的金鑰，核准時會重複登錄，`VerificationKeyStore` 以先登錄者為準；本單未加防重複檢查，待確認。

**紅綠流程**：先加入 `SigningKeyRegistrationApproval.feature` 與步驟，`dotnet test` 因端點不存在而失敗（申請回應 404）；實作後行為 Scenario 轉綠，之後才勾選本單驗收項目。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj --filter "FullyQualifiedName~簽章金鑰登錄申請"
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

**範圍限制**：退役與撤銷簽章金鑰（14 單）、管理操作稽核（15 單）不在本單；Gateway 相關項目略過（非完成）。
