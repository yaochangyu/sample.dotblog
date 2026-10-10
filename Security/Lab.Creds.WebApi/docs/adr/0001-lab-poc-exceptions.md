# 本 lab 刻意偏離一般開發規則的例外

本專案是驗證 server-to-server 保護機制的 lab／PoC，不是正式產品。為保持簡單，刻意偏離 api.template 的一般開發規則；以下例外只在本 lab 有效，正式採用前必須逐項補齊。

- **手寫 Minimal API，不用 NSwag 產生**：API First 的契約是 `poc/AuthSpike/doc/openapi.yml`，但它只作契約文件，端點由手寫 Minimal API 實作，沒有 Codegen 流水線，也沒有自動檢查程式與契約一致。
- **狀態儲存為程序內物件**：`OrderStore`、`NonceReplayStore`、`TrustRegistry` 以程序內 `Dictionary` 加 `lock` 實作，不是 EF InMemory，也不是跨程序儲存。「多執行個體」以同一程序內共用物件模擬，因此 60 秒撤銷與防重放的一致性只在單程序內驗證，不代表跨機器或跨程序部署的實測。
- **不採分層與框架慣例**：未採 Controller → Handler → Repository 分層，也未採 Result Pattern 與 Serilog；診斷輸出為 ASP.NET 框架的 Console 記錄。
- **不做 Gateway**：呼叫端直接呼叫業務 API，由業務 API 以 TLS 連線憑證自行驗證。Gateway 相關驗收項目在 issue 中標為「略過（非完成）」，對應 `@ignore` Scenario 保留為未來契約。
- **管理介面**：授權伺服器以 `/admin/*` 端點提供信任名單讀取、Client 停用、憑證與簽章金鑰登錄申請的核准與拒絕、退役與撤銷，以及管理操作稽核查詢（`GET /admin/audit-records`，測試縫）。管理員以專屬自簽 mTLS 憑證驗證，憑證由 lab 啟動時產生並登錄為管理角色。路徑、申請欄位、回應格式與稽核保存期皆為 lab 暫定值，待使用者確認。限制：信任名單、登錄申請與稽核紀錄皆為**程序內**儲存，只在**單程序**內驗證；未提供持久化、跨程序一致性、多人覆核與管理員憑證頒發流程，正式採用前須逐項補齊。
- **自簽 mTLS**：只用自簽用戶端憑證（`self_signed_tls_client_auth`），不做 CA 型 `tls_client_auth`；憑證每次啟動在記憶體產生，OpenIddict 金鑰為 ephemeral。
- **登錄申請端點未驗證呼叫者**：`POST /client-certificate-requests` 與 `POST /signing-key-requests` 不要求任何身分，也不要求私鑰持有證明，且沒有待核准數量上限，任何人都能為既有 Client 送出申請並灌入待核准佇列。申請本身不改變信任名單、不能取得 Token，風險限於待核准佇列。申請管道（工單、人工轉交或自助入口）與其驗證方式尚未決定，屬待使用者確認；正式採用前須補上申請管道的身分驗證與數量限制。
- **稽核與套用的先後順序**：管理操作先寫入稽核（`accepted`），再套用變更。稽核寫入失敗時不套用並回 503（AC-22）；但若套用本身丟出例外，稽核已記為 `accepted` 且不會補記失敗結果。此不一致未在 lab 修正，因為重現需要套用路徑的故障注入點，lab 範圍內沒有此注入點；正式採用前須補上套用失敗的補記。
- **`/admin/clients/{clientId}/disable` 為 Spec 未要求的管理操作**：保留為 lab 暫定，待使用者確認是否納入正式範圍。

沿用的已核准決定不變：Token 效期 300 秒、HTTP Message Signatures 規則、撤銷上限 60 秒。

刻意保留的結構（程式碼異味檢視後決定不拆）：
- **`SpikeRuntime` 維持單一組合根**：它同時負責啟動與憑證／簽章金鑰輪替，但輪替操作與啟動共用同一組私有字典與登錄狀態，拆開需引入額外共享物件並改動大量測試呼叫點，風險大於效益；`SignedRequestVerifier` 則已把 Signature-Input 解析抽出為 `SignatureInputParser`。
- **`SpikeTrust`、`SpikeDbContext` 保留**：前者是所有 HTTPS 端點與測試共用的信任根驗證，後者是 OpenIddict EF Core 儲存區所需的具名 DbContext，皆非無用的間接層。
- **`AuthServerHost` 維持單一檔案（管理路由與 Token 端點不拆）**：管理路由與 `/connect/token` 共用同一個 `WebApplication`、同一個 OpenIddict 登錄、同一組信任名單字典與決策閘門（`decisionGate`），拆成獨立類別需把這些共享狀態全部以參數傳入，風險大於效益。已知代價：核准與拒絕的骨架（查找、404、非待核准回 409）在憑證與簽章金鑰之間重複，`RunAdministrativeOperationAsync` 有 7 個參數，對象 `Subject` 與 `Reason` 為字串（它們就是稽核紀錄的對外值，改為 enum 不改變行為）。這些在 lab 範圍內接受，正式採用前再重構。
