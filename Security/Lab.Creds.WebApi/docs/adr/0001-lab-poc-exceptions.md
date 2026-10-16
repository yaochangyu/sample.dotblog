# 本 lab 刻意偏離一般開發規則的例外

本專案是驗證 server-to-server 保護機制的 lab／PoC，不是正式產品。為保持簡單，刻意偏離 api.template 的一般開發規則；以下例外只在本 lab 有效，正式採用前必須逐項補齊。

- **手寫 Minimal API，不用 NSwag 產生**：API First 的契約是 `poc/AuthSpike/doc/openapi.yml`，但它只作契約文件，端點由手寫 Minimal API 實作，沒有 Codegen 流水線，也沒有自動檢查程式與契約一致。
- **狀態儲存為程序內物件**：`OrderStore`、`NonceReplayStore`、`TrustRegistry` 以程序內 `Dictionary` 加 `lock` 實作，不是 EF InMemory，也不是跨程序儲存。「多執行個體」以同一程序內共用物件模擬，因此 60 秒撤銷與防重放的一致性只在單程序內驗證，不代表跨機器或跨程序部署的實測。
- **不採分層與框架慣例**：未採 Controller → Handler → Repository 分層，也未採 Result Pattern 與 Serilog；診斷輸出為 ASP.NET 框架的 Console 記錄。
- **不做 Gateway**：呼叫端直接呼叫業務 API，由業務 API 以 TLS 連線憑證自行驗證。Gateway 相關驗收項目在 issue 中標為「略過（非完成）」，對應 `@ignore` Scenario 保留為未來契約。
- **管理介面**：授權伺服器以 `/admin/*` 端點提供信任名單讀取、Client 停用、憑證與簽章金鑰登錄申請的核准與拒絕、退役與撤銷，以及管理操作稽核查詢（`GET /admin/audit-records`，測試縫）。管理員以專屬自簽 mTLS 憑證驗證，憑證由 lab 啟動時產生並登錄為管理角色。路徑、申請欄位、回應格式與稽核保存期皆為 lab 暫定值，待使用者確認。限制：信任名單、登錄申請與稽核紀錄皆為**程序內**儲存，只在**單程序**內驗證；未提供持久化、跨程序一致性、多人覆核與管理員憑證頒發流程，正式採用前須逐項補齊。
- **自簽 mTLS**：只用自簽用戶端憑證（`self_signed_tls_client_auth`），不做 CA 型 `tls_client_auth`；憑證每次啟動在記憶體產生，OpenIddict 金鑰為 ephemeral。
- **申請者驗證的限制**：`POST /client-certificate-requests` 與 `POST /signing-key-requests` 只接受已登錄且有效的 Client，以其現有已核准憑證經 mTLS 驗證後，為自己提出申請，並須附上新私鑰的持有證明；未驗證者、替他人申請與證明失敗的申請一律拒絕且不建立待核准紀錄。限制如下：全新 Client 的首次登錄不在此端點範圍，其信任來源仍是管理員建立；每個 Client 的待核准數上限（5 筆，憑證與簽章金鑰合計）、持有證明的表示方式與逾期處理（自提出起 24 小時，逾期後視為已過期、不能再被核准或拒絕、不佔額度）皆為 lab 暫定值，待使用者確認；持有證明的簽署內容綁定已驗證的申請者 Client、申請者目前憑證指紋、新憑證或金鑰指紋、時間戳與一次性 nonce；伺服器以自己的時鐘判定持有證明時間窗 60 秒（lab 暫定、待使用者確認），持有證明 nonce 一次性：已受理的證明其 nonce 登錄於程序內，保存至時間窗結束，同一份證明重送被拒絕且不新增待核准紀錄、不佔額度。申請與稽核紀錄為**程序內**儲存，只在**單程序**內驗證。申請管道（工單、人工轉交或自助入口）尚未決定，屬待使用者確認；正式採用前須補上申請管道的審查、持久化與逾期處理。
- **稽核與套用的先後順序**：管理操作先寫入稽核（`accepted`），再套用變更。稽核寫入失敗時不套用並回 503（AC-22）；但若套用本身丟出例外，稽核已記為 `accepted` 且不會補記失敗結果。此不一致未在 lab 修正，因為重現需要套用路徑的故障注入點，lab 範圍內沒有此注入點；正式採用前須補上套用失敗的補記。
- **`/admin/clients/{clientId}/disable` 為 Spec 未要求的管理操作**：保留為 lab 暫定，待使用者確認是否納入正式範圍。

沿用的已核准決定不變：Token 效期 300 秒、HTTP Message Signatures 規則、撤銷上限 60 秒。

刻意保留的結構（程式碼異味檢視後決定不拆）：
- **`SpikeRuntime` 維持單一組合根**：它同時負責啟動與憑證／簽章金鑰輪替，但輪替操作與啟動共用同一組私有字典與登錄狀態，拆開需引入額外共享物件並改動大量測試呼叫點，風險大於效益；`SignedRequestVerifier` 則已把 Signature-Input 解析抽出為 `SignatureInputParser`。
- **`SpikeTrust`、`SpikeDbContext` 保留**：前者是所有 HTTPS 端點與測試共用的信任根驗證，後者是 OpenIddict EF Core 儲存區所需的具名 DbContext，皆非無用的間接層。
- **`AuthServerHost` 維持單一檔案（管理路由與 Token 端點不拆）**：管理路由與 `/connect/token` 共用同一個 `WebApplication`、同一個 OpenIddict 登錄、同一組信任名單字典與決策閘門（`decisionGate`），拆成獨立類別需把這些共享狀態全部以參數傳入，風險大於效益。已知代價：核准與拒絕的骨架（查找、404、非待核准回 409）在憑證與簽章金鑰之間重複，`RunAdministrativeOperationAsync` 有 7 個參數，對象 `Subject` 與 `Reason` 為字串（它們就是稽核紀錄的對外值，改為 enum 不改變行為）。這些在 lab 範圍內接受，正式採用前再重構。

## 申請者驗證的審查判斷（審查意見後記錄）

以下為審查後判定為 lab 刻意保留、不在本次改動的項目。正式採用前須逐項決定是否修正。

- **申請稽核的操作者標示（已處理，lab 暫定、待使用者確認）**：稽核的 actorStatus 只區分「已驗證管理員」與「未驗證」（此判定不變），被拒絕的嘗試與成功受理的申請者一律標為「未驗證」，不把宣稱的 Client 記成已驗證。為分辨「憑證通過但替他人申請」與「完全匿名」，另新增獨立欄位 `applicantAuthenticated`（只有申請端點有值；匿名為 `false`）與 `applicantClientId`（通過時為經 mTLS 驗證的申請者 Client，未通過時為 `null`）。不採用「已驗證申請者」作為 actorStatus 的新值，以免弱化「被拒絕的嘗試不記為已驗證」的既有判定。欄位名稱與語意待使用者確認。
- **持有證明的重送（已處理，lab 暫定）**：持有證明已綁定申請者身分、新指紋、時間戳（時間窗 60 秒）與一次性 nonce；同一份證明重送被拒絕且不佔額度，時間窗與 nonce 表示方式待使用者確認。nonce 登錄僅在程序內，多執行個體或重啟後的一致性未驗證。
- **逾期（lab 暫定 24 小時，待使用者確認）**：待核准申請自提出起 24 小時逾期，到達即視為已過期：不能再被核准或拒絕（409 `request_expired`）、不再佔用上限，Client 可再提出申請；管理員查詢回 `expired`。逾期時間與逾期規則待使用者確認。逾期判定以注入的時鐘實作（測試不真的等待）；持有證明的時間窗仍以系統時間判定，維持既有行為。被拒絕或證明失敗的嘗試不佔額度。
- **待核准計數的重複**：`CertificateRegistrationRequests.CountPending` 與 `SigningKeyRegistrationRequests.CountPending` 各自實作（兩者為不同型別，本體各僅數行），lab 接受此重複。
- **`AuthenticateApplicant` 留在 `AuthServerHost`**：它需要讀取 host 擁有的信任名單字典，移到 `TrustRegistry` 需對外露出該字典，lab 接受此 feature envy。
- **步驟前綴 「19 單」「20 單」「21 單」**：Reqnroll 步驟綁定為全域，以單號前綴避免跨 feature 的步驟文字衝突；可讀性代價 lab 接受。
- **`PendingRequestCleanup` 的 `AfterScenario`**：為全域清理，以 `ReferenceEquals` 排除已失效的執行環境。測試支援層的刻意設計，lab 接受。

## 雙呼叫者：同一業務 API 同時接受服務與使用者（08 單）

本節補記「同一建立訂單端點同時接受服務呼叫與使用者呼叫」的 lab 例外與限制。服務路徑的既有保護（mTLS 綁定、請求簽章、nonce、撤銷與 fail closed）不變，使用者路徑不降低其要求。

- **測試使用者帳號代替真實登入**：使用者 Token 以 password grant 由同一個授權伺服器為測試使用者簽發，沒有互動式登入介面、使用者註冊或使用者管理。
- **使用者 Token 不綁用戶端憑證、不簽章**：這是設計決定，不是遺漏。使用者路徑只靠 Token 內省（簽發者、audience、有效期、呼叫者類型與使用者有效狀態）與寫入的防重放 nonce（`X-Request-Nonce`）保護。
- **程序內儲存**：使用者狀態（停用）、防重放與冪等紀錄沿用程序內儲存，只在單程序內驗證，不代表跨程序或跨機器的實測。
- **不做委託與 token exchange**：服務不能以自身身分代表使用者呼叫；需代表使用者的呼叫另行設計。Gateway 與其信任契約同樣不在範圍內。

### 待使用者確認的 lab 暫定值

以下值皆已在實作與測試中使用，但未經使用者核准，正式採用前須逐項確認或修改：

- 使用者 Token 的效期（沿用既有 300 秒）與表示方式（格式）。
- 使用者防重放機制（`X-Request-Nonce` 經 `INonceReplayStore`，缺少或重放 401，狀態故障 503）。
- 角色與使用者資料範圍（使用者只能讀寫自己的訂單，他人或服務建立的訂單回應 404）。
- 撤銷傳播（使用者停用或 Token 撤銷於 60 秒撤銷上限內生效，沿用既有上限）。
- 錯誤格式（Token 無效 401、查證服務故障 503 `verification_unavailable`、資料範圍拒絕 404、限定單一類型的操作由另一類型呼叫回應 403）。

### 雙呼叫者審查後的修正與刻意保留（審查意見後記錄）

本次修正：服務路徑拒絕宣稱 `caller_type=user` 與 `user:` 前綴 client_id 的 Token（避免與使用者業務範圍鍵碰撞）；使用者路徑內省回應格式不符改判為 401（非查證故障）；內省加上 10 秒逾時（lab 暫定、待使用者確認），逾時回應 503；建立訂單宣告 `X-Request-Nonce` 標頭；稽核失敗的分支改由單一輔助方法處理。

以下為審查判斷後刻意保留、本次未改動的項目：

- **服務專屬操作缺位**：目前寫入端點（`POST /orders`、`POST /orders/{orderId}/cancel`）對服務與使用者皆開放，只有 `/orders/me/{orderId}` 限定使用者。規格第 24 項為「可以」只開放一種呼叫者，並非必須；是否新增只允許服務呼叫的操作待使用者決定，決定前不新增。
- **使用者角色模型缺位**：測試使用者帳號由 password grant 取得 `orders.read` 與 `orders.write`，沒有角色區分。角色與各操作允許的呼叫者清單屬規格「待具體 API 設計」項目，lab 暫定。
- **撤銷延遲**：快取上限（`CallerVerifier` 的 lifetime，lab 暫定、不超過 Token 有效期）即為撤銷延遲的上限，於快取期間內的撤銷最多延遲一個快取週期；使用者停用則每次請求直接讀取，不受快取影響。
- **稽核的已驗證身分並存**：`VerifiedClientId`（僅服務，沿用 09 單斷言）與 `VerifiedCallerIdentity`（服務與使用者）語意重疊，兩者並存為 lab 暫定值，待使用者確認後再合併。
- **測試帳密與 Scopes 位置**：`UserCallers` 內的測試帳密與可核發 scope 與 `TestUsers` 尚未分離，屬 lab 測試資料；將來擴充前再拆分。
- **組合根長度**：`OrdersApiHost.StartAsync` 同時負責 DI 組態、稽核信封、使用者防重放、服務簽章驗證與路由。拆分留待正式採用前重構，lab 接受。
- **命名**：`ClaimedCaller.FromPrincipal` 與 `OrdersApiHost` 的 `ClaimedCallerOf` 命名風格不一致，只影響可讀性，lab 接受。
