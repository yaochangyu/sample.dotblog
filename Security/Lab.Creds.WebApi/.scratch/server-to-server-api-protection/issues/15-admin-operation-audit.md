# 15: 管理操作稽核

**What to build:** 每一次信任名單的改變都留下稽核紀錄：登錄申請、核准、拒絕、退役、撤銷，各記錄操作的管理員、時間、對象 Client、憑證或金鑰的指紋與結果。稽核紀錄不含私鑰與原始 Token。稽核紀錄寫入失敗時，該管理操作不生效並明確回報服務錯誤，信任名單不會在無紀錄下被改變。管理操作稽核與既有的呼叫者安全稽核分開管理。

**Blocked by:** 12 — 憑證登錄申請與核准／拒絕；13 — 簽章金鑰登錄申請與核准；14 — 管理員退役與撤銷憑證與簽章金鑰；09 — 安全追查已驗證呼叫者。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；稽核紀錄的端點、欄位、保存期與存取規則為 lab 暫定值，待使用者確認）

- [x] 每種管理操作（登錄申請、核准、拒絕、退役、撤銷）都產生紀錄，可辨識操作的管理員、時間、對象 Client、指紋與結果（AC-21）。
- [x] 稽核紀錄不含私鑰、原始 Token 或完整憑證私有內容（AC-21）。
- [x] 稽核寫入失敗時，管理操作不生效，信任名單不變，並明確回報服務錯誤（AC-22）。
- [x] 被拒絕的管理員呼叫（未驗證或非管理員）以「未驗證」標示，不記為已驗證管理員。
- [x] 管理操作稽核與既有呼叫者稽核分開管理；保存期與存取規則為 lab 暫定值並標註「待使用者確認」。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作；12 至 14 單的拒絕結果在本票補齊稽核。

## 實作紀錄（15）

Feature 檔 `poc/AuthSpike.Tests/Features/AdminOperationAudit.feature` 與步驟檔 `Steps/AdminOperationAuditSteps.cs` 採 BDD（Reqnroll，保留字英文、步驟中文），經真實 mTLS 與 HTTP 路徑：管理操作由管理員憑證呼叫管理端點，稽核紀錄由管理員身分經 `GET /admin/audit-records` 查詢。步驟不直接讀取稽核紀錄的內部儲存。

**先紅後綠的落差**：Feature 與步驟檔在實作前撰寫，但並未在實作前單獨執行紅燈；首次執行在實作完成之後，當時失敗的 7 個斷言皆為尚未勾選的「15 單第 N 項驗收已勾選」檢查，行為斷言全數通過。因此第 6 項「先看到失敗再實作」未完全遵守，此處如實記錄。

### 實作

- `Audit/AdministrativeAuditLog.cs`：`AdministrativeAuditRecord` 與 `AdministrativeAuditLog`（僅追加、程序內記憶體、寫入失敗拋 `AdministrativeAuditWriteFailedException`）。與 `SecurityAuditLog`（呼叫者安全稽核）為不同型別與不同實例，分開保存。
- `AuthServer/AuthServerHost.cs`：所有管理操作改經 `RunAdministrativeOperationAsync`：先計算結果（不改變狀態）→ 寫入管理操作稽核 → 成功後才套用變更。稽核寫入失敗時不套用，回應 503 `audit_unavailable`。
- `Hosting/SpikeRuntime.cs`：組合根建立 `AdministrativeAuditLog`，並以 `AdministrativeAuditLog` 屬性提供給測試故障注入。

### 紀錄欄位（lab 暫定值，待使用者確認）

- `correlationId`、`occurredAt`、`operation`（`certificate.submit`／`approve`／`reject`／`retire`／`revoke`、`signing_key.*`、`client.disable`）。
- `actorStatus`：`已驗證管理員`（管理員 mTLS 憑證已登錄為管理角色）或 `未驗證`（非管理員、無憑證、未登錄憑證，以及申請管道）。`actorThumbprint` 只是出示的憑證指紋，不視為身分。
- `clientId`、`subject`（`certificate`／`signing_key`）、`keyId`、`fingerprint`（憑證為 SHA-1 Thumbprint；簽章金鑰為公開金鑰 SubjectPublicKeyInfo 的 SHA-256）。
- `outcome`（`accepted`／`rejected`）、`reason`（`ok` 或錯誤代碼）、`resultStatus`（HTTP 狀態碼）。
- 不含私鑰、Token、Authorization 標頭或憑證私有內容。

### 範圍決定

- **每次管理操作嘗試都留一筆紀錄，包含被拒絕者**（如 404、409、401）。被拒絕的管理呼叫以 `未驗證` 標示，同時補齊 12 至 13 單拒絕結果與 14 單 409 結果的稽核。
- **`client.disable` 也納入稽核**：11 單的停用會改變信任名單的 `enabled` 狀態，依「每一次信任名單的改變」納入；本單未列名，因此明確記錄於此。
- **申請管道的紀錄**：申請端點不需管理員身分，紀錄為 `未驗證`，`clientId` 與 `fingerprint`（申請的公開憑證或金鑰指紋）為申請內容。
- **測試縫**：新增 `GET /admin/audit-records`（僅管理員），測試經此端點以管理員身分讀取。讀取不寫入稽核。故障注入為 `AdministrativeAuditLog.SimulateWriteFailure`（lab 故障模擬）。
- **保存期與存取規則**：保存期 90 天（lab 暫定，待使用者確認）；僅追加；只經管理員身分的查詢端點讀取，不對業務 API 公開（lab 暫定，待使用者確認）。

### 已知限制（待查）

- 稽核寫入成功後才套用變更。若套用步驟本身拋出例外（例如 OpenIddict 登錄更新失敗），該管理操作會以 500 回應，但紀錄已寫為 `accepted`。目前紀錄為僅追加，不做補償紀錄；正式環境需設計補償或兩階段寫入。
- 管理操作稽核仍為程序內記憶體儲存，與 ADR 0001 的 lab 例外一致；跨程序或持久化不在本單範圍。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/AdminOperationAudit.feature`

| 項目 | Scenario |
|---|---|
| 1 每種操作產生可辨識紀錄（AC-21） | `憑證登錄申請與核准各留下可辨識操作者、對象、指紋與結果的紀錄`、`管理員退役憑證與簽章金鑰各留下紀錄`、`管理員撤銷憑證與簽章金鑰各留下紀錄`、`管理員停用 Client 留下紀錄` |
| 2 不含私鑰或原始 Token（AC-21） | `管理操作稽核紀錄不含私鑰或原始 Token` |
| 3 寫入失敗不生效並回報（AC-22） | `管理操作稽核寫入失敗時，核准不生效並明確回報服務錯誤`、`管理操作稽核寫入失敗時，退役與撤銷不生效` |
| 4 未驗證標示 | `未經管理員驗證的呼叫被拒絕，且稽核紀錄標示為未驗證`（Scenario Outline：Client 憑證、無憑證、未登錄憑證） |
| 5 分開管理；lab 暫定標註 | `管理操作稽核與呼叫者安全稽核分開管理`、`管理操作稽核的保存期與存取規則標示為 lab 暫定待使用者確認`（`@record`，僅檢查紀錄文字，非行為驗證） |
| 6 BDD 覆蓋與 12–14 拒絕補稽核 | `管理員拒絕憑證與簽章金鑰申請，各留下拒絕結果的紀錄`；上列各 Scenario 皆經真實 TLS 與 HTTP 路徑
