# 11: 管理介面與管理員身分驗證

**What to build:** 授權伺服器提供只給管理員使用的管理介面。管理員以專屬的用戶端憑證經 mTLS 驗證，並登錄為管理角色；未經管理員驗證的呼叫一律被拒絕，信任名單不會改變。管理員身分與 Client 身分完全分開：管理憑證不能用來取得業務 Token 或呼叫業務 API，Client 憑證也不能呼叫管理介面。這張票先建立後續登錄核准都要經過的管理入口，並以管理介面的拒絕與隔離行為作為可單獨驗證的成果。

**Blocked by:** 02 — 呼叫服務取得 Token 並經可信入口呼叫 API。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；管理員憑證登錄方式與管理介面路徑為 lab 暫定值，待使用者確認；本單無 Gateway 驗收項目）

- [x] 管理介面只接受以專屬管理憑證經 mTLS 驗證的管理員（lab 暫定、待使用者確認：管理員憑證於環境啟動時產生並登錄為管理角色，Spec 未定義其正式頒發與保管流程）。
- [x] 無憑證、未登錄憑證或持 Client 憑證呼叫管理介面時被拒絕（401），信任名單不變（AC-15）。
- [x] 管理憑證嘗試取得業務 Token 或呼叫業務 API 時被拒絕（AC-20）。
- [x] 管理介面與業務 API 的驗證結果各自獨立，不共用已驗證呼叫者的判斷。
- [x] 以真實 mTLS 與 HTTP 路徑驗證，不以永遠成功的替身取代管理員驗證；每個驗收項目至少對應一個 BDD Scenario（Feature、Scenario、Given、When、Then 用英文，步驟用中文）。
- [x] 管理介面的路徑與回應格式為 lab 暫定值並標註「待使用者確認」；不實作 Gateway 相關項目。

對應驗收：AC-15、AC-20，並沿用 AC-23 的 Client 隔離精神（本單不涉及登錄核准，AC-16～AC-19、AC-21～AC-23 留待 12～16 單）。

## 實作紀錄（11）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；Feature 檔 `AdministratorInterfaceAndIdentity.feature` 先寫並確認紅燈（測試專案無法編譯，缺少 `AdministratorCertificate` 等管理介面成員）後才實作轉綠。延用既有 `TrustRegistry`、`AuthServerHost`、`CallerVerifier`、`SpikeRuntime` 的組合根模式。

### 管理員身分與登錄

- 管理員專屬 mTLS 憑證（自簽、與任何 Client 憑證分開）於 `SpikeRuntime.StartAsync` 每個執行環境產生，CN 為 `administrator-{environmentName}`，並以 `TrustRegistry.RegisterAdministratorCertificate` 登錄為管理角色。
- 管理員身分只看出示的 mTLS 憑證指紋是否登錄為管理角色（`IsAdministrator`），不經 Token 查證，也不共用業務 API 的 `CallerVerifier` 結果。
- 管理員憑證不在任何 Client 的 JWKS 中；授權伺服器的 `/connect/token` 另有明確拒絕條件（管理員憑證不得取得 Token）。

### 管理介面

- `GET /admin/trust-list`：回傳信任名單快照（各 Client 的啟用狀態與已登錄 mTLS 憑證指紋），依 clientId 排序。
- `POST /admin/clients/{clientId}/disable`：停用 Client（呼叫 `TrustRegistry.DisableClient`），回應 200；Client 不存在回應 404。
- 非管理員（無憑證、未登錄憑證、Client 憑證）一律回應 401 `administrator_authentication_required`，且不執行任何變更；管理員檢查先於 Client 存在性檢查，避免未驗證者探測 Client 清單。
- 管理介面與業務 API 同在授權伺服器程序內，但驗證路徑分開：管理介面只查管理員角色；業務 API 的 `CallerVerifier` 對管理員憑證明確回應 Rejected，不進入快取或 introspection。

### 管理員與 Client 隔離

- 管理員憑證呼叫 `/connect/token`（任一 client_id）：不核發 Token。
- 管理員憑證持 Client 的 Token 呼叫建立訂單：業務 API 回應 401，訂單數不增加。
- Client 憑證（持有效 Token）呼叫建立訂單：回應 201；同一 Client 憑證呼叫管理介面：回應 401。

### lab 暫定、待使用者確認

- 管理員憑證的登錄方式：啟動時於組合根登錄（未定義正式頒發、保管與多人覆核，Spec 列為待具體設計項目）。
- 管理介面路徑 `/admin/trust-list`、`/admin/clients/{clientId}/disable` 與 JSON 回應格式。
- 非管理員的拒絕狀態碼採 401（未以 403 區分「已認證但非管理員」）。
- 管理介面的 Client 停用操作僅作為本單驗證拒絕行為的可觀察變更點；12～14 單的登錄、退役與撤銷流程尚未實作。

### 範圍限制（明確列出）

- 本單不實作 Gateway；Gateway 相關項目不在驗收範圍。
- 管理員憑證為記憶體內產生，不持久化；沒有管理員憑證的正式頒發流程。
- 管理操作稽核（AC-21、AC-22）與登錄申請（AC-16～AC-18）未在本單實作，留待 12～16 單。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/AdministratorInterfaceAndIdentity.feature`

| 項目 | Scenario |
|---|---|
| 1 只接受專屬管理憑證經 mTLS 驗證的管理員 | `管理員憑證與 Client 憑證分屬不同身分`、`已驗證管理員可讀取信任名單` |
| 2 無憑證／未登錄／Client 憑證被拒絕，信任名單不變（AC-15） | `未經管理員驗證的呼叫讀取信任名單被拒絕`（Scenario Outline）、`未經管理員驗證的呼叫停用 Client 被拒絕且信任名單不變`（Scenario Outline） |
| 3 管理憑證不能取得業務 Token 或呼叫業務 API（AC-20） | `管理憑證不能取得業務 Token`、`管理憑證不能呼叫業務 API` |
| 4 管理介面與業務 API 驗證結果各自獨立 | `管理介面與業務 API 的驗證結果各自獨立`（另含 `管理員停用 Client 後信任名單與授權結果同步反映`） |
| 5 真實 mTLS／HTTP 路徑與 BDD 覆蓋 | 所有行為 Scenario 皆經真實 TLS 連線；`11 單的實作紀錄說明可重現驗證方式`（`@record`） |
| 6 路徑與回應格式標註待確認 | `管理員憑證的登錄方式與管理介面的 lab 暫定值已標註待確認`（`@record`） |

**紀錄型證據（`@record`，非行為驗證）**：`管理員憑證的登錄方式與管理介面的 lab 暫定值已標註待確認`、`11 單的實作紀錄說明可重現驗證方式` 只檢查本紀錄的文字或勾選狀態，不證明系統行為。

### 驗證方式（可重現）

```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

僅執行本單情境：

```
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj --filter "FullyQualifiedName~管理介面與管理員身分驗證"
```
