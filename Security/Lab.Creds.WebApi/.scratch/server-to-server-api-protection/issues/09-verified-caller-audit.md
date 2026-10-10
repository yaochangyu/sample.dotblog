# 09: 安全追查已驗證呼叫者

**What to build:** 維護者可從一筆請求追查已驗證 Client、憑證／簽章金鑰、操作與結果；失敗請求不能偽造可信身分紀錄，紀錄也不洩漏認證或敏感業務資訊。

**Blocked by:** 03 — 業務 API 驗證原始呼叫端請求簽章。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；本單無 Gateway 驗收項目；lab 暫定值待使用者確認）

- [x] 成功的呼叫紀錄包含已驗證 Client、必要金鑰識別、操作、結果與關聯識別。
- [x] 認證或驗簽失敗時，請求宣稱的 Client 身分標示為未驗證，不與已驗證呼叫者混淆。
- [x] 紀錄不含原始 Token、私鑰或完整敏感 Body；檢查錯誤、診斷與簽章驗證紀錄也符合要求。
- [x] 防止簽章基底或包含 Token 的標頭透過除錯資訊洩漏。
- [x] 安全稽核紀錄與必要業務去重狀態分開管理；保存期及存取規則已記錄為 lab 暫定值（待使用者確認）。
- [x] 透過成功、失敗及偽造身分的整合情境，驗證紀錄可正確關聯且不含禁止資料。
- [x] 紀錄失敗不被靜默吞掉或假報成功，依已確認的專案錯誤處理契約（503 `audit_unavailable`）回報。

對應驗收：AC-14。

## 實作紀錄（09）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理。Scenario 先寫並確認紅燈（12 個情境中 10 失敗，另 2 個因稽核紀錄為空而空轉通過，已改為必須非空）後才實作轉綠。延用 `OrdersApiHost` 的查證與簽章閘門、`SpikeRuntime` 與既有測試環境；Gateway 不在本 lab 範圍。

### 稽核紀錄內容

- 每筆 `/orders` 業務請求恰一筆紀錄，由決策點寫入：查證閘門（`caller_rejected`、`caller_verification_unavailable`）、簽章閘門（`signature_verified` 為 accepted；`signature_rejected`、`signature_replayed`、`replay_state_unavailable`）、授權後補記（`denied` 與 `status_403` 等）。
- 欄位：`CorrelationId`（即 ASP.NET `TraceIdentifier`，同時以 `X-Correlation-Id` 回應標頭返回）、`OccurredAt`、`Operation`（方法與路徑，不含查詢字串）、`Outcome`、`Reason`、`VerifiedClientId`（僅簽章通過時）、`SignatureKeyId`（僅簽章通過時，如 `orders-client-sig-1`）、`UnverifiedTokenClientId`（Token 宣稱的身分，標示為未驗證）、`PresentedClientIdHeader`（`X-Client-Id` 宣稱值，去除非常規字元並截至 64 字元，標示為未驗證）、`CertificateThumbprint`（TLS 觀察到的憑證指紋）。
- 不記錄：原始 Token、`Authorization`／`Signature`／`Signature-Input` 標頭值、簽章基底、私鑰、Body。

### 已驗證身分與未驗證宣稱

- `VerifiedClientId` 只在簽章通過時填入；查證通過但簽章失敗、重放、無授權範圍等情況，Token 或標頭中的 Client 一律放在未驗證欄位。
- 重放（`signature_replayed`）保守地一律標示為未驗證。

### 寫入失敗

- 紀錄先於業務處理寫入。寫入失敗時回應 503 `{"error":"audit_unavailable"}`，不建立訂單、不回 201，也不改回 401。
- 查證失敗（例如 Token 無效）的拒絕紀錄寫入失敗時，同樣回 503 `audit_unavailable`，不回 401。

### 分離管理、保存與存取（lab 暫定，待使用者確認）

- `SecurityAuditLog` 為獨立物件，與 `NonceReplayStore`、`OrderStore` 分開。
- 保存期 90 天（lab 暫定、待使用者確認）；寫入時清除過期紀錄。
- 存取規則（lab 暫定、待使用者確認）：僅追加（append-only），不提供修改或刪除；不經業務 API 公開（`/audit` 回 404）；僅維護者程序內讀取。

### lab 暫定、待使用者確認

- 稽核保存期 90 天。
- 存取規則：僅追加、不經業務 API 公開、僅程序內讀取。唯一例外：回應開始送出時，對同一筆紀錄一次性補記 `ResultStatus`。
- 最終業務結果（code review 補修）：簽章通過後仍先寫 accepted 紀錄（寫入失敗即 fail closed，不進入業務處理）；紀錄新增 `ResultStatus`（最終 HTTP 狀態碼，如 201／409／422／503），由稽核信封在 `Response.OnStarting` 補記，故呼叫端收到回應時紀錄已完整，且每筆請求仍只有一筆紀錄。補記不含 Body 或業務內容，不洩漏機敏資料。補記發生在回應已決定之後，不改變業務結果；若無對應紀錄（如稽核寫入失敗）則略過。新增 Scenario：成功 201、重放 401、無 scope 403、冪等鍵衝突 422 均驗證 `ResultStatus`。
- `PresentedClientIdHeader` 保留理由（code review 評估）：spec 未要求，但此欄位把「宣稱 X-Client-Id」與已驗證 Client 明確分開，可追查冒用他人身分的嘗試，且既有 Scenario 以它證明偽造宣稱不會被當成已驗證；內容已去除非常規字元並限長 64，風險低，故保留。
- 稽核紀錄以記憶體保存，不持久化。
- 稽核寫入失敗回應格式沿用 `verification_unavailable` 的格式，錯誤代碼為 `audit_unavailable`。

### 尚未具備的保護（本階段明確範圍限制）

- 稽核紀錄為進程內記憶體，不持久化，不跨程序或跨機器；多執行個體共用同一個 `SecurityAuditLog` 物件。
- 「不洩漏」的保證以測試檢查回應、標頭與稽核紀錄內容驗證；ASP.NET 框架本身的主控台記錄未設 log sink，未納入檢查。
- 本單無 Gateway 驗收項目；Gateway 可信身分標頭不在本單範圍。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/VerifiedCallerAudit.feature`

| 項目 | Scenario |
|---|---|
| 1 成功紀錄內容 | `成功驗證的建立訂單請求留下含已驗證 Client、金鑰識別、操作、結果與關聯識別的紀錄` |
| 2 失敗標示未驗證 | `未簽章的請求不被記為已驗證，宣稱的 Client 標示為未驗證`、`以他人簽章金鑰簽署的請求不與已驗證呼叫者混淆`、`無效 Token 的請求宣稱的 Client 不被記為已驗證`、`重送同一份已簽章請求的紀錄為 rejected 且不記為已驗證`、`無授權範圍的 Token 被拒絕，錯誤回應與紀錄不洩漏 Token` |
| 3 不含原始 Token、私鑰、敏感 Body | `稽核紀錄與成功回應不含原始 Token、簽章私鑰與完整敏感 Body`、`錯誤回應與簽章驗證紀錄不含 Token、Signature 標頭或簽章基底` |
| 4 不洩漏簽章基底或 Token 標頭 | `錯誤回應與簽章驗證紀錄不含 Token、Signature 標頭或簽章基底`、`無授權範圍的 Token 被拒絕，錯誤回應與紀錄不洩漏 Token` |
| 5 分開管理、保存期與存取規則 | `稽核紀錄與防重放儲存分開管理，保存期與存取規則已記錄為 lab 暫定` |
| 6 整合情境關聯 | `成功、失敗與偽造身分的情境可依關聯識別正確對應且不含禁止資料` |
| 7 寫入失敗不靜默 | `簽章通過但稽核紀錄寫入失敗時拒絕建立訂單，回報 audit_unavailable`、`呼叫者查證失敗的拒絕紀錄寫入失敗時回報 audit_unavailable 而非 401` |

### 驗證方式（可重現）

- `dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
- `dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --no-build --filter "FullyQualifiedName~AuthSpike.Tests.Features.安全追查已驗證呼叫者Feature"`
- 全量：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --no-build`
