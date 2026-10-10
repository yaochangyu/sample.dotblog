# 16: 管理員核准的整體驗收與文件

**What to build:** 以管理員核准憑證與簽章金鑰為主題，端到端驗證 AC-15 到 AC-23 全部情境，並更新領域詞彙與 ADR，讓「信任名單由管理員核准」的決定與尚未決定的項目有明確紀錄。這張票是收尾驗收，不新增功能；若驗收發現缺漏，回到對應票修正。

**Blocked by:** 15 — 管理操作稽核。

**Status:** resolved（受控測試環境驗證；完整測試套件 237 通過、0 失敗、2 略過（既有 `@ignore` Gateway 情境）；待使用者確認項目見下方）

- [x] AC-15 到 AC-23 每一條都有對應的 BDD Scenario 並實際通過，完整測試套件 0 失敗。
- [x] 從申請、核准、Client 取得 Token 並呼叫、退役或撤銷、稽核查詢，有一條端到端 Scenario 串起來。
- [x] `GLOSSARY.md` 補上本需求新增的詞彙（例如管理員、登錄申請），並與既有「Client 身分」「已驗證呼叫者」區分。
- [x] `docs/adr/0001-lab-poc-exceptions.md` 補記管理介面的 lab 例外與限制（單程序、程序內儲存等）。
- [x] 列出所有「待使用者確認」的 lab 暫定值與尚未決定的項目（申請傳遞管道、多人覆核、管理員憑證頒發、管理介面格式、稽核保存期）。
- [x] 不實作 Gateway 相關項目；沿用已確認的 lab 範圍決定。

## 實作紀錄（16）

Feature 檔 `poc/AuthSpike.Tests/Features/AdminApprovalAcceptance.feature` 與步驟檔 `Steps/AdminApprovalAcceptanceSteps.cs` 採 BDD（Reqnroll，保留字英文、步驟中文），經真實 mTLS 與 HTTP 路徑。本票不新增產品功能；端到端流程只呼叫既有管理端點與業務 API。

### 先紅後綠的紀錄

- 先寫 Feature 檔，未寫步驟前執行 `dotnet test`：10 個情境全數失敗，原因為步驟未定義（undefined）。
- 補上步驟檔後，端到端情境通過：申請、核准、取得 Token 並呼叫、退役原始憑證、撤銷新憑證、稽核查詢全部成立。此通過來自 11 至 15 單既有的行為，本票沒有新增產品行為，因此端到端情境的「行為紅燈」並不存在，這點如實記錄。
- 記錄型情境（AC 對應表、GLOSSARY、ADR、待確認清單）在補文件前執行為紅燈，補文件後轉綠。

### AC 對應表（AC-15 至 AC-23）

| AC | 對應情境（主要） |
|---|---|
| AC-15 | 未經管理員驗證的呼叫讀取信任名單被拒絕 |
| AC-16 | 提交申請後為待核准且無法取得 Token |
| AC-17 | 管理員核准後憑證進入信任名單並可取得綁定該憑證的 Token |
| AC-18 | 管理員拒絕後憑證不生效且留下可查詢的拒絕結果 |
| AC-19 | 撤銷憑證後既有連線上的請求在 60 秒內被阻擋 |
| AC-20 | 管理憑證不能呼叫業務 API |
| AC-21 | 憑證登錄申請與核准各留下可辨識操作者、對象、指紋與結果的紀錄 |
| AC-22 | 管理操作稽核寫入失敗時，核准不生效並明確回報服務錯誤 |
| AC-23 | 核准某 Client 的申請不改變其他 Client 的憑證與權限 |

AC-19 與 AC-15 另含「非管理員不能執行」與「未驗證呼叫停用 Client 被拒絕」等同類情境，見各自 feature 檔的 Scenario Outline。端到端情境為 `申請、核准、取得 Token 呼叫、退役、撤銷與稽核查詢串成一條端到端流程`（`AdminApprovalAcceptance.feature`）。

### 術語與 ADR

- `GLOSSARY.md` 新增「信任名單與管理」段落：**管理員**、**登錄申請**，並說明與 **Client 身分**、**已驗證呼叫者** 的區分。
- `docs/adr/0001-lab-poc-exceptions.md` 新增「管理介面」lab 例外與限制：程序內儲存、單程序驗證、無持久化、無跨程序一致性、無多人覆核與管理員憑證頒發流程。

### 待使用者確認（lab 暫定值與尚未決定項目）

以下項目在 lab 中採簡單預設或刻意未定，不視為已核准：

- 申請傳遞管道：工單、人工轉交或自助入口，尚未決定（規格「範圍外」）。
- 多人覆核（four-eyes）：目前單一管理員即可核准，尚未決定。
- 管理員憑證頒發：目前由 lab 啟動時產生自簽管理憑證，正式頒發與保管流程尚未決定。
- 管理介面格式：`/admin/*` 路徑、申請欄位、回應格式與 HTTP 錯誤格式為 lab 暫定值。
- 稽核保存期：沿用 15 單的 90 天 lab 暫定值，存取規則亦待確認。
- 申請有效期與逾期處理：尚未決定。

### Gateway

本票不實作 Gateway。Gateway 相關項目沿用 ADR 0001 與既有 `@ignore` 情境，不計為完成。

### 可重現驗證

於 `poc/` 目錄執行：`dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj`。本票的端到端情境與記錄型情境位於 `AdminApprovalAcceptance` feature。

### 實際結果

- `dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj`：Build succeeded，0 Warning(s)、0 Error(s)。
- `dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj`（完整套件）：Passed! — Failed: 0、Passed: 237、Skipped: 2（既有 `@ignore` Gateway 情境）、Total: 239，約 6 分 44 秒。
- 先前一次完整執行的唯一失敗為本票第 1 項勾選檢查（尚未勾選時），勾選後重跑即為上列結果；並非行為失敗。
