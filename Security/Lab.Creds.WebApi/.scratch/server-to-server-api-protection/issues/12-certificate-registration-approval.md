# 12: 憑證登錄申請與核准／拒絕

**What to build:** Client 的用戶端憑證要加入信任名單時，先提交只含公開憑證的登錄申請，經管理員核准後才生效。申請狀態區分待核准、已核准、已拒絕；待核准與已拒絕的憑證不能取得 Token，核准後該憑證可取得綁定自身憑證的 Token。管理員也可以拒絕申請，拒絕不改變信任名單。各 Client 的申請互相隔離，核准某 Client 不會影響其他 Client 的憑證或權限。申請內容不得含私鑰。

**Blocked by:** 11 — 管理介面與管理員身分驗證。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；申請端點、申請欄位與管理端點路徑為 lab 暫定值，待使用者確認；申請傳遞管道不在本單範圍；本單無 Gateway 驗收項目）

- [x] 提交申請後狀態為待核准；此時該憑證無法取得 Token，也無法通過任何 Client 認證（AC-16）。
- [x] 管理員核准後憑證進入信任名單並生效，Client 可取得綁定該憑證的 Token（AC-17 憑證部分）。
- [x] 管理員拒絕申請後不生效，並留下可查詢的拒絕結果（AC-18）；稽核內容於 15 單補齊。
- [x] 核准某 Client 的申請不改變其他 Client 的憑證、金鑰與權限（AC-23）。
- [x] 申請只接受公開憑證，含私鑰的申請被拒絕。
- [x] 申請者不能自行核准自己的申請；非管理員不能核准或拒絕。
- [x] 申請的傳遞管道（工單、人工轉交或自助入口）不在本票範圍，不自行假設；申請內容欄位為 lab 暫定值並標註「待使用者確認」。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作；沿用既有 mTLS 憑證綁定與撤銷行為，不降低既有保護。

## 實作紀錄（12）

**範圍與端點（lab 暫定、待使用者確認）**：
- 提交申請：`POST /client-certificate-requests`，本體 `{ clientId, publicCertificatePem }`，回應 202 與 `requestId`、狀態 `pending`；未知 Client 回應 404。
- 管理員查詢：`GET /admin/client-certificate-requests/{requestId}`，回應申請狀態（`pending`／`approved`／`rejected`）與指紋；只有管理員可查。
- 管理員核准／拒絕：`POST /admin/client-certificate-requests/{requestId}/approve`、`.../reject`；非待核准的申請回應 409（`request_not_pending`），已拒絕者不可再核准。
- 申請內容欄位（lab 暫定、待使用者確認）：只含 `clientId` 與公開憑證 PEM；含私鑰的內容回應 400（`private_key_not_allowed`），不解析、不儲存。
- 申請管道（工單、人工轉交或自助入口）未定義，本 lab 端點暫不設驗證，申請本身不能取得 Token，也不改變信任名單。

**行為**：
- 申請僅記錄於 `CertificateRegistrationRequests`（程序內，`lock` 保護）；待核准與已拒絕的憑證不在 OpenIddict JWKS 內，`/connect/token` 以既有 mTLS 登錄判斷拒絕（AC-16、AC-18）。
- 核准時先更新該 Client 的信任名單與 OpenIddict JWKS，成功後才標示已核准（AC-17 憑證部分）；核准與拒絕以 `SemaphoreSlim` 序列化。
- `ReplaceTrustedCertificatesAsync` 只更新指定 Client 的登錄，其他 Client 的憑證、權限不變（AC-23）。
- `SpikeRuntime` 的輪替（08 單）改以授權伺服器的信任名單為基礎，避免輪替時丟失已核准的憑證。

**紅綠流程**：先加入 `CertificateRegistrationApproval.feature` 與步驟，`dotnet test` 因端點不存在而失敗（11 個 Scenario 列）；實作後行為 Scenario 轉綠，之後才勾選本單驗收項目。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj --filter "FullyQualifiedName~憑證登錄申請"
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

**待查（未於本單結案）**：實作過程中，同一 Client 的多張憑證若 subject（CN）相同，OpenIddict 的 self-signed TLS 驗證曾拒絕其中一張（`ID2197`，invalid_client）；測試改以唯一 CN 避開。原因尚未於 OpenIddict 原始碼確認，推測與 JWKS 金鑰識別（kid）衝突有關，列為後續待查，正式採用前須確認。

**範圍限制**：簽章金鑰登錄與核准（13 單）、退役與撤銷（14 單）、管理操作稽核（15 單）不在本單；申請者身分尚未記錄於稽核（15 單補齊）；Gateway 相關項目略過（非完成）。
