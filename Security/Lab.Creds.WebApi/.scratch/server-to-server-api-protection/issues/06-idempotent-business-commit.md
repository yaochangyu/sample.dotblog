# 06: 業務操作重試與併發只提交一次

**What to build:** 已授權且通過請求保護的同一業務操作，因重試、併發或回應遺失不會重複提交本地業務副作用；呼叫者可取得既有結果或處理狀態。

**Blocked by:** 04 — 跨執行個體防重放並支援合法重簽重試；05 — 隔離 Client 操作權限與業務資料範圍。

**Status:** resolved（10 項驗收全部達成；受控 lab 測試環境驗證；本單無 Gateway 驗收項目；lab 暫定值待使用者確認，見實作紀錄）

- [x] 示範操作的穩定業務識別、Idempotency Key 範圍、業務內容比對規則、最長重試期及保存期取得確認。（lab 暫定、待使用者確認：業務識別 client_id 加 orderReference；業務內容比對 item 與 quantity；最長重試期 10 分鐘；Idempotency-Key 保存 24 小時。）
- [x] 相同識別与相同業務內容的已完成操作提供既有結果，不重做副作用。
- [x] 相同操作仍處理中時提供處理狀態，不啟動第二次執行。
- [x] 相同業務識別或 Idempotency Key 卻不同內容時明確拒絕，不覆寫既有操作。
- [x] 更換 Idempotency Key 仍是相同業務操作時，由穩定業務識別阻止重複提交。
- [x] 業務內容比對排除重試會改變的 nonce、簽章與 Token。
- [x] 去重狀態與本地業務提交一致處理；實測跨執行個體併發、提交前後當機及提交後回應遺失。
- [x] Idempotency Key 保存涵蓋約定重試期；短期紀錄過期後仍維持業務唯一性。
- [x] 觀察持久業務結果證明只提交一次，不僅以回應相同作為證據；純查詢不強套業務去重。
- [x] 明確界定本地提交保證，不宣稱下游或跨系統 exactly-once。

對應驗收：AC-09、AC-10、AC-11。

## 實作紀錄（06）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；14 個 Scenario 先寫並確認紅燈（全數失敗）後才實作轉綠。延用 `OrderStore`、`OrdersApiHost`、第二個建立訂單 API 執行個體、`SpikeRuntime` 與既有測試環境。示範操作為「建立訂單」。

### 業務識別、Idempotency Key 範圍與比對規則（lab 暫定、待使用者確認）

- 業務識別為 client_id 加 orderReference：請求 Body 的 `orderReference`（必填，最長 64 字元，client_id 範圍內唯一）。重試時必須沿用同一值；業務識別不隨 Idempotency-Key 或 nonce 改變。
- Idempotency-Key 範圍：client_id 加 Idempotency-Key。Key 只用來定位請求嘗試，並指向一個業務識別。
- 業務內容比對為 item 與 quantity：只比對 `item` 與 `quantity`。排除 nonce、簽章與 Token；Idempotency-Key 也不參與比對。
- 最長重試期：10 分鐘（lab 暫定，待使用者確認）。
- 保存期：Idempotency-Key 紀錄保存 24 小時（涵蓋最長重試期，lab 暫定，待使用者確認）；業務識別與其訂單結果於 lab 記憶體中永久保存，不因 Key 紀錄過期而失去唯一性。
- 處理中租約：30 秒（lab 暫定，待使用者確認）。租約到期後同一業務操作可由新嘗試接手。

### 去重狀態與本地提交（對應 1 至 5、7 項）

- 判斷與寫入在 `OrderStore` 內同一把鎖完成：訂單、業務識別的完成狀態與 Key 紀錄一併寫入，不存在「訂單已寫入但去重狀態未完成」的中間狀態。
- 同一業務識別且內容相同、已完成：回應 201 與既有訂單編號，帶標頭 `Idempotent-Replayed: true`，不新增訂單、不增加建立計數。
- 同一業務識別仍處理中（租約有效）：回應 409 `{"error":"operation_in_progress","status":"processing"}`，不啟動第二次執行。
- 同一 Idempotency-Key 對應不同業務識別：回應 422 `idempotency_key_conflict`，不覆寫。
- 同一業務識別但內容不同：回應 422 `order_reference_conflict`，不覆寫既有訂單。
- 租約接手：業務識別的處理中租約到期後，新嘗試可接手並提交；舊嘗試以 AttemptId 比對，若已被接手則不寫入（fencing）。舊嘗試被拒絕的路徑本身未另列 Scenario，目前由「提交前當機後租約到期」情境涵蓋接手成功的路徑。
- 業務提交無法確認（提交前當機或提交後回應遺失）：回應 503 `business_commit_unavailable`，呼叫端以同一業務識別重試。

### 併發、當機與回應遺失（對應 7 項）

- 跨執行個體併發：兩個建立訂單 API 執行個體共用 `OrderStore`；10 組各自新簽章與新 Idempotency-Key 的同一業務請求交替送出，只產生 1 筆訂單，回應只包含 201 或 409。
- 提交前當機：注入故障於寫入前，不產生訂單（業務訂單數 0）；同一業務識別重送回應 409；租約到期後重送回應 201，且業務訂單數為 1。
- 提交後回應遺失：注入故障於寫入後，訂單已存在但回應為 503；以同一 Idempotency-Key 與新 nonce 重送回應既有訂單（201 重播），業務訂單數仍為 1。
- 處理中暫停：於提交前暫停，第二個請求回應 409 `processing`；放行後第一個請求回應 201，業務訂單數為 1。

### 保存期與持久業務結果（對應 8、9 項）

- Key 紀錄過期後（測試以 hook 模擬保存期結束）：同一 Key 與新 nonce 重送仍回應既有訂單，業務訂單數為 1。
- 持久業務結果證據：以業務識別查得的訂單數（`CountByOrderReference`）、訂單內容與 `GET /orders/{id}` 查詢結果共同證明只提交一次；不只以回應相同為證據。
- 純查詢：`GET /orders/{id}` 不經業務去重，兩次查詢皆回應 200，且查詢前後去重紀錄數不變。

### 本地提交保證界定（對應 10 項）

- 保證：本業務服務內，同一業務操作（client_id 加 orderReference）的本地訂單只提交一次，涵蓋併發、提交前後當機與提交後回應遺失，於本服務的記憶體狀態內成立。
- 不宣稱下游或跨系統 exactly-once：下游系統或跨系統的 exactly-once 不在本單保證內；下游冪等協定另訂，本單不宣稱。
- 本地提交與去重狀態於同一寫入單位完成；不涵蓋跨程序或跨機器持久化（見範圍限制）。

### 契約與既有測試調整

- `poc/AuthSpike/doc/openapi.yml` 先於實作更新（API First）：`CreateOrderRequest` 新增必填 `orderReference`；201 增加重播標頭；新增 400、409、422；503 增加 `business_commit_unavailable`。
- 既有測試的建立訂單 Body 改為每次產生新的 `orderReference`，使各 Scenario 互不去重；重試情境沿用原 Body（業務內容與業務識別不變）。
- 04 單的跨執行個體重試情境（`ReplayProtectionAndRetry.feature`）原先預期兩次建立（業務去重屬 06 單）；依 06 單語意改為重試不重複建立，訂單總數為 1。防重放行為未變。

### Scenario 對應表

| 驗收 | Scenario |
|---|---|
| 1 | 業務識別、Idempotency Key 範圍與保存期已記錄為 lab 暫定值，且契約涵蓋業務識別 |
| 2 | 相同業務識別與相同內容的已完成操作回傳既有結果，不重做副作用 |
| 3 | 相同操作仍處理中時回應處理中狀態，且不啟動第二次執行 |
| 4 | 相同 Idempotency Key 但業務內容不同時明確拒絕，且不覆寫既有操作；相同業務識別但業務內容不同時明確拒絕，且不覆寫既有訂單 |
| 5 | 更換 Idempotency Key 仍是相同業務操作時，由業務識別阻止重複提交 |
| 6 | 業務內容比對排除重試會改變的 nonce、簽章與 Token |
| 7 | 提交前當機不產生訂單，租約到期後重試只提交一次；提交後回應遺失時重試回傳既有結果，且只提交一次；跨執行個體併發提交同一業務操作只提交一次 |
| 8 | Idempotency Key 保存期涵蓋最長重試期，紀錄過期後仍維持業務唯一性 |
| 9 | 持久訂單與查詢結果證明只提交一次，而非僅以回應相同為證據；純查詢不套用業務去重，查詢不寫入去重紀錄 |
| 10 | 本地提交保證與下游冪等責任分開記錄 |

### 驗證方式

- 紅燈：新 Feature 先寫入並建置，`dotnet test --filter` 執行 14 個 Scenario，全數失敗（業務行為與實作紀錄斷言）。
- 綠燈：`poc/AuthSpike.Tests` 執行 `dotnet build` 與 `dotnet test --no-build`，結果見下方「最終測試結果」。

### 最終測試結果

- `dotnet build`（poc/AuthSpike.Tests）：0 Error(s)，0 Warning(s)，Build succeeded。
- `dotnet test --no-build`（poc/AuthSpike.Tests）：Passed! - Failed: 0, Passed: 126, Skipped: 2, Total: 128（約 4 分 15 秒）。2 個 skipped 為既有的 Gateway 下游通道情境（本 lab 不實作）。
- 06 相關 14 個 Scenario 全數通過；04 跨執行個體重試情境（依 06 語意更新為訂單總數 1）與 05、07、09 既有情境全數通過。

### lab 暫定值（待使用者確認）

- 業務識別欄位名稱與範圍：`orderReference`，client_id 範圍內唯一。
- 業務內容比對：`item` 與 `quantity`（大小寫與字串完全相同才視為相同）。
- 最長重試期：10 分鐘。
- Idempotency-Key 保存期：24 小時。
- 處理中租約：30 秒。
- 故障回應代碼：`business_commit_unavailable`（503）；重播回應使用 201 與 `Idempotent-Replayed: true`。
- 去重狀態為記憶體保存，不持久化。

### 範圍限制

- 業務去重狀態與訂單皆為單一行程的記憶體保存（lab）；不涵蓋多程序、多機器持久化與資料庫交易。
- 「當機」以注入故障模擬（寫入前中止、寫入後回應遺失），不是真實程序終止。
- 下游或跨系統 exactly-once 不在範圍內。
- 本單無 Gateway 驗收項目（Gateway 不在本 lab 範圍）。

### 取消訂單不要求 Idempotency-Key（code review 補修）

- 查證：取消訂單（`POST /orders/{orderId}/cancel`）把訂單狀態設為 cancelled，天生冪等；handler 本就不讀 Idempotency-Key，也沒有去重語意，強制要求只增加呼叫端負擔。
- 決定：取消訂單不再要求 Idempotency-Key。簽章規則改為只有建立訂單（`POST /orders`）涵蓋 `idempotency-key`；取消訂單涵蓋 `content-digest` 與 `content-type`。openapi.yml 同步移除 cancel 的 Idempotency-Key 標頭。
- Scenario：`取消訂單天生冪等，不附 Idempotency-Key 的已簽章取消請求也成功，重複取消結果相同`（CompleteProtectionAcceptance.feature）。
