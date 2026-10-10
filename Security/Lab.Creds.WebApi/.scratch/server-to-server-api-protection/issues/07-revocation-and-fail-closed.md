# 07: 60 秒撤銷與查證故障時拒絕處理

**What to build:** 管理者撤銷 Client、Token、憑證或簽章金鑰後，所有驗證端最多 60 秒內阻擋後續使用；查證故障且沒有時效內有效快取時，不接受業務請求。

**Blocked by:** 03 — 業務 API 驗證原始呼叫端請求簽章。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證，Gateway 不在本 lab 範圍，本單無 Gateway 驗收項目）

- [x] 確認撤銷操作、狀態同步、各層快取及故障回應契約；不新增未經核准的管理 UI。
- [x] 分別撤銷 Client、Token、mTLS 憑證、簽章金鑰，後續請求都能被阻擋。
- [x] 撤銷操作生效起到各驗證端拒絕的總延遲不超過 60 秒，包含同步、各層快取與處理延遲。
- [x] 實測多執行個體及既有連線上的後續請求，不只驗證重新建立 TLS 連線。
- [x] 停用 Client 或輪替憑證不能被當成既有 Token 已自動失效；接受判斷確實涵蓋所有必要有效狀態。
- [x] 查證服務故障且仍有時效內快取時，不延長快取有效期限；無有效快取時 fail closed。
- [x] 查證故障明確回報暫時無法驗證的服務錯誤，不偽裝成已確認的憑證無效或靜默放行。
- [x] 被拒絕請求不進入業務副作用；撤銷不宣稱回滾已提交的業務操作。
- [x] 提供測量起點、時間與拒絕結果作為 60 秒門檻證據。

對應驗收：AC-04、AC-05。

## 實作紀錄（07）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；Scenario 先寫並確認紅燈後才實作轉綠。延用 `TrustRegistry`、`CallerVerifier`、第二個建立訂單 API 執行個體與既有測試環境。

### 撤銷契約與狀態同步

- 撤銷操作：lab 進程內管理操作（`TrustRegistry.DisableClient`、`RevokeCertificate`、`RevokeSigningKey`；Token 撤銷為授權伺服器 `RevokeAccessTokenAsync`）。不新增管理 UI。
- Client 停用、mTLS 憑證撤銷、簽章金鑰撤銷：業務 API 每次請求直接讀取 `TrustRegistry`，不經快取，立即生效。
- Token 撤銷：授權伺服器 introspection 回傳非 active；業務 API 的 `CallerVerifier` 快取成功查證結果，因此最多延遲一個快取上限（10 秒，見下方 lab 暫定）。
- 既有連線：每個業務請求都重新判斷（快取或 `TrustRegistry`），不因 TCP/TLS 連線已建立而略過。
- 停用 Client 或撤銷憑證不會使既有 Token 被 introspection 判為無效；接受判斷另檢查 Client、憑證與簽章金鑰狀態。輪替重疊流程屬 08 單，本單只涵蓋撤銷。
- 撤銷不回滾已提交的訂單（測試確認撤銷後既有訂單仍為 `active`）。

### 查證快取與故障回應契約

- 快取：`CallerVerifier` 只快取查證成功的結果，有效期不超過快取上限與 Token 到期時間；到期即重新查證。
- 故障：查證服務無法連線（或 OpenIddict 回報 `server_error`）且沒有有效快取時，回應 503 `{"error":"verification_unavailable"}`，不回應 401，不進入業務處理，不建立訂單。
- 查證失敗不寫入快取，因此不延長既有快取的有效期限。
- 契約：`poc/AuthSpike/doc/openapi.yml` 已更新（API First），撤銷與 `verification_unavailable` 說明寫入 info 與各 503 回應。

### 測量與 60 秒門檻證據

- 測量起點：管理者撤銷操作執行前的 `DateTimeOffset.UtcNow`（`WhenAdministratorRevokes` 寫入 `_revokedAt`）。
- 拒絕時間：業務 API 首次回應 401 的時間（每 250 毫秒送出一次已簽章查詢，直到拒絕或超過 60 秒）。
- 60 秒門檻：每次測量以 `elapsed <= 60 秒` 斷言；超過即失敗。
- 實測（`[撤銷生效延遲]`，2026-10-10 完整測試執行）：
  - Client、mTLS 憑證、簽章金鑰撤銷：耗時約 0.01 至 0.03 秒（不經快取）。
  - Token 撤銷：主要與第二個執行個體的首次拒絕耗時約 9.99 至 10.25 秒，受 10 秒查證快取上限約束，低於 60 秒門檻。

### lab 暫定、待使用者確認

- 查證快取上限 10 秒（`SpikeRuntime.DefaultVerificationCacheLifetime`）。
- `@short-cache` 測試環境使用 4 秒快取（曾因下述「授權伺服器停止後每個請求延遲約 15 秒」暫拉長為 30 秒，根因修正後已還原）。
- 故障錯誤代碼 `verification_unavailable` 與 503 回應格式（lab 暫定，待 API 設計確認）。
- 撤銷以進程內管理操作進行，不新增管理 UI（正式管理方式待使用者核准）。

### 尚未具備的保護（本階段明確範圍限制）

- 多程序或分散式撤銷同步（推送或輪詢）未實作；多執行個體測試共用同一 `TrustRegistry` 與防重放儲存，同步延遲為進程內即時。
- 60 秒證據來自單一測試程序，不代表跨機器部署的實測。
- 本單不實作 Gateway；撤銷 Gateway 通道與偽造標頭屬其他單（本單無 Gateway 驗收項目）。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/RevocationAndFailClosed.feature`

| 項目 | Scenario |
|---|---|
| 1 契約、狀態同步、快取與故障回應 | `撤銷操作、狀態同步、快取與故障回應契約已記錄` |
| 2 分別撤銷 Client、Token、mTLS 憑證、簽章金鑰 | `撤銷<對象>後後續請求在 60 秒內被阻擋`（Token、Client、mTLS 憑證、簽章金鑰） |
| 3 總延遲不超過 60 秒 | `撤銷<對象>後後續請求在 60 秒內被阻擋`、`撤銷 Token 後兩個執行個體於 60 秒內拒絕後續請求` |
| 4 多執行個體與既有連線 | `撤銷 Token 後兩個執行個體於 60 秒內拒絕後續請求`、`既有連線與第二個執行個體上的後續請求同樣被阻擋` |
| 5 停用或撤銷不使既有 Token 自動失效 | `撤銷<對象>後既有 Token 仍為有效，但接受判斷仍拒絕`（Client、mTLS 憑證、簽章金鑰） |
| 6 快取不延長、無快取時 fail closed | `查證服務故障時快取有效期間內可接受，且失敗不延長快取有效期限`、`查證服務故障且沒有有效快取時拒絕處理且不建立訂單` |
| 7 明確回報暫時無法驗證 | `查證服務故障時快取有效期間內可接受，且失敗不延長快取有效期限`、`查證服務故障且沒有有效快取時拒絕處理且不建立訂單` |
| 8 被拒絕請求無副作用、撤銷不回滾 | `撤銷後被拒絕的寫入不建立訂單，撤銷不回滾已提交的訂單`、`查證服務故障且沒有有效快取時拒絕處理且不建立訂單` |
| 9 測量起點、時間與拒絕結果 | `60 秒門檻的測量起點與拒絕結果已記錄於實作紀錄`（實際撤銷 Token、輪詢至拒絕並斷言測得延遲小於 60 秒，另檢查紀錄文字） |

**紀錄型證據（`@record`，非行為驗證）**：下列 Scenario 只檢查本紀錄的文字或勾選狀態，屬紀錄型證據，標籤為 `@record`，不冒充行為驗證：
- 撤銷操作、狀態同步、快取與故障回應契約已記錄

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

### 授權伺服器停止後請求延遲約 15 秒：根因與證據

- 根因：業務 API 以 `AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)` 設定了預設驗證 scheme，ASP.NET Core 的驗證中介軟體因此在每個請求進入 `CallerVerifier` 之前就先自動向授權伺服器 introspection，使查證快取形同虛設（快取命中仍會 introspection，結果被丟棄）。授權伺服器停止後，OpenIddict 7 的 System.Net.Http 預設重試（指數退避）依序在 +0、+1、+2、+4、+8 秒重試連線被拒，約 15 秒後才失敗，之後請求才進入閘門，所以「首個請求約 15 秒才抵達」。與 mTLS、憑證鏈、撤銷檢查、IPv6/IPv4 解析無關（連線被拒為即時）。
- 證據（暫時加入的時間戳日誌與 EventListener，已移除）：
  - 同一條 TCP 連線（來源埠不變）上，Kestrel `RequestStart` 為 11:26:24.350，但閘門日誌 11:26:39.372；其間 `POST /connect/introspect` 重試時間為 24.353、25.360、27.361、31.363、約 39.37，每次 `ConnectFailed 10061`（Connection refused，耗時約 1 毫秒），間隔 1、2、4、8 秒。
  - 用戶端 `HttpClient` 伺服器憑證驗證耗時 0 至 1 毫秒，且沒有新建 TLS 連線；卡住的是伺服器端 introspection，不是用戶端。
  - 修正前（4 秒快取）快取命中的查詢耗時約 15 秒並得到 503；修正後同一查詢耗時約 30 毫秒並得到 200。
- 修正：不設定預設 Authenticate scheme（僅設定 Challenge 與 Forbid scheme 維持 401/403 行為），`CallerVerifier` 在快取未命中時才明確指定 scheme 呼叫 `AuthenticateAsync`。快取命中不再呼叫授權伺服器。
- 行為性 Scenario：@short-cache 情境新增「快取命中的查詢未等待授權伺服器而在 2 秒內完成」，修正前失敗、修正後連跑 3 次通過。
- 仍存在（非本次範圍）：快取未命中且授權伺服器不可用時，單一請求需等約 15 秒重試後才回 503；是否縮短重試（`SetHttpResiliencePipeline`）待使用者決定。

### 未決事項與阻擋

- 無阻擋。上述 lab 暫定值已實作並標註，待使用者確認。
- 已查明並修正：授權伺服器停止後約 15 秒的請求延遲，根因見下節。

### 查證故障判定補強（code review 補修）

- `CallerVerifier` 對 `AuthenticateAsync` 拋出的任何例外一律視為 verification_unavailable（503，fail closed），不再只限網路層例外；OpenIddict 回報的 `server_error`（introspection 回 5xx、非 JSON、空內容）同樣是 503。只有授權伺服器明確回應 Token inactive 才是 401。
- Scenario：`introspection 回應 <情境> 時回 503 而非 401`（IntrospectionFault.feature，以假授權伺服器回 502、500、壞 JSON、空內容為 503，inactive 為 401）。這些情境在補強前已為 503，屬回歸保護；逾時情境未納入（OpenIddict 預設逾時 100 秒，測試成本過高），由任何例外皆 503 的規則涵蓋。
