# 本 lab 刻意偏離一般開發規則的例外

本專案是驗證 server-to-server 保護機制的 lab／PoC，不是正式產品。為保持簡單，刻意偏離 api.template 的一般開發規則；以下例外只在本 lab 有效，正式採用前必須逐項補齊。

- **手寫 Minimal API，不用 NSwag 產生**：API First 的契約是 `poc/AuthSpike/doc/openapi.yml`，但它只作契約文件，端點由手寫 Minimal API 實作，沒有 Codegen 流水線，也沒有自動檢查程式與契約一致。
- **狀態儲存為程序內物件**：`OrderStore`、`NonceReplayStore`、`TrustRegistry` 以程序內 `Dictionary` 加 `lock` 實作，不是 EF InMemory，也不是跨程序儲存。「多執行個體」以同一程序內共用物件模擬，因此 60 秒撤銷與防重放的一致性只在單程序內驗證，不代表跨機器或跨程序部署的實測。
- **不採分層與框架慣例**：未採 Controller → Handler → Repository 分層，也未採 Result Pattern 與 Serilog；診斷輸出為 ASP.NET 框架的 Console 記錄。
- **不做 Gateway**：呼叫端直接呼叫業務 API，由業務 API 以 TLS 連線憑證自行驗證。Gateway 相關驗收項目在 issue 中標為「略過（非完成）」，對應 `@ignore` Scenario 保留為未來契約。
- **自簽 mTLS**：只用自簽用戶端憑證（`self_signed_tls_client_auth`），不做 CA 型 `tls_client_auth`；憑證每次啟動在記憶體產生，OpenIddict 金鑰為 ephemeral。

沿用的已核准決定不變：Token 效期 300 秒、HTTP Message Signatures 規則、撤銷上限 60 秒。
