# 01: 確認 auth 技術選型與接入契約

**What to build:** 以最小可執行驗證確認授權伺服器與可信入口的候選方案能提供既定接入保證，將證據與必要契約交給使用者決策，解除後續實作阻擋。本單是研究／決策前置單，不代表正式業務功能完成。

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] 閱讀已指定的 api.template 開發規則及其必讀指南，確認全專案採 API First 或 Code First，不自行假設。
- [x] 將 OpenIddict 視為候選而非已選定產品；核對候選的正式版本、授權條件、維護狀態及與 ASP.NET Core 10 整合方式。
- [x] 以真實協定及密碼驗證路徑展示 mTLS Client Credentials 核發憑證綁定 Opaque Token、introspection 與 API 憑證綁定判斷；無憑證及不同憑證呼叫失敗。
- [x] 證據區分正式版本支援、需整合功能與尚未支援能力，不以 dev 文件或永遠成功的替身作為可用性證明。
- [x] 確認 Gateway 候選與信任契約（本 lab 不實作，略過，不阻擋；見下方未決事項）。
- [x] 取得使用者對產品選型、API 開發方式及首個示範業務操作的確認；缺乏可行方案時明確列出阻擋，不降級既定保護。
- [x] 記錄可重現驗證方式、已確認契約與未決事項；不手刻 OAuth 協定作為未經核准的替代品。

## 實作紀錄（01）

本紀錄由 spike 實際執行結果整理；未實際執行的項目不列為已確認。

### 已確認的決策（使用者）

- 授權伺服器：OpenIddict（候選已驗證，見下）。
- API 開發方式：API First；契約 `poc/AuthSpike/doc/openapi.yml`（`POST /orders`）。Codegen 流水線（NSwag／Refitter）尚未建立，見未決事項。
- 首個示範業務操作：建立訂單。

### OpenIddict 候選核對（NuGet 7.7.1 實際數據）

- 正式版本：7.7.1，發佈於 2026-09-17，非預覽版；2026 年內持續釋出（7.2.0 至 7.7.1）。
- 授權條件：Apache-2.0（套件 nuspec）。
- 與 ASP.NET Core 10 整合：`OpenIddict.AspNetCore`、`OpenIddict.Validation.SystemNetHttp` 皆為 net10.0 組件，spike 以 .NET 10 建置執行。
- 支援證據分級：
  - 已實際驗證（BDD 通過）：RFC 8705 自簽 TLS 用戶端認證（`self_signed_tls_client_auth`）、憑證綁定 Opaque Token（`UseReferenceAccessTokens` + `UseClientCertificateBoundAccessTokens`）、introspection 含 `cnf`、API 端以 introspection 驗證並由 validation 堆疊強制 cnf 綁定。
  - 需整合、尚未驗證成功：RFC 8705 PKI 用戶端認證（`tls_client_auth`）。依文件以各 Client 下級 CA 放入 JWKS 並以 `ExtraStore` 提供中間 CA 嘗試，皆回應 `ID2197`（憑證無效），未找到可用組態；本 spike 不宣稱支援。
  - 未驗證：Gateway 終止 mTLS 的整合（本 lab 不實作，見未決事項）。
  - 注意：本 spike 參照的 OpenIddict 文件為 dev 分支，僅用於定位用法；可用性以上述實際執行為準。

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet run --project AuthSpike/AuthSpike.csproj   # 另可手動啟動，Ctrl+C 停止
```

測試以 Reqnroll（Gherkin 英文關鍵字、繁體中文步驟）執行，`BeforeTestRun` 以真實 TCP／TLS（Kestrel，`AllowCertificate`）啟動授權伺服器（127.0.0.1 隨機埠）與建立訂單 API，憑證於記憶體中以 `CertificateRequest` 產生。結果：26 個 Scenario 通過、1 個 `@ignore`（Gateway，本 lab 不實作）略過、0 失敗（`Total: 27`，`Failed: 0`）。

### 已確認的契約（spike 範圍）

- Client 以自簽用戶端憑證於 token 端點做 mTLS 認證；憑證公開部分登錄於 Client 的 JWKS；不使用 Client Secret。
- 未附憑證、其他 CA 簽發、未登錄自簽憑證，皆無法取得 Token（AC-01 相關）。
- Opaque Token（非 JWT）綁定取得時的憑證，內省結果 `cnf.x5t#S256` 與憑證指紋一致。
- 同一憑證搭配 Token 建立訂單回應 201；無憑證、其他 CA、未登錄自簽憑證、其他已登錄 Client 的憑證，皆回應 401（AC-02、AC-03 相關）。
- API 不信任公開請求自行提供的 `X-Client-Cert-Sha256` 標頭，只以 TLS 連線憑證判斷（防入口標頭偽造的最小證據）。
- 撤銷：本 spike 未實作撤銷機制（服務停用、Token／憑證撤銷與 60 秒上限屬 issue 07）。

### 未決事項與阻擋

- **Gateway 候選與信任契約（本 lab 不實作，略過，不阻擋）**。Gateway 相關驗收項目（下游通道認證、防入口繞過、驗證資訊來源、原始簽章內容保留）標註為本 lab 不實作；業務 API 直接以 TLS 連線憑證自行驗證。未驗證 Gateway 終止 mTLS；對應 `@ignore` Scenario 保留為未來契約。
- **決定：本 lab 只用自簽用戶端憑證（`self_signed_tls_client_auth`），不做 CA 型 PKI（`tls_client_auth`）**。`ID2197` 排查不屬本 lab 範圍；若日後廠商接入需 CA 管理，另行立單。
- **需決策：Client 憑證的登錄與輪替流程**：自簽方式需為每個 Client 登錄其公開憑證；輪替與撤銷流程屬 issue 07/08。
- Spike 限制（非正式環境設定）：OpenIddict 金鑰為 ephemeral；儲存為 EF Core InMemory；憑證為測試用 RSA 2048 自簽；HTTPS 信任只驗證鏈不驗證主機名稱；未建立 NSwag／Refitter 的 API First 產生流程。
### 驗收 checkbox 對應 Scenario

每個驗收 checkbox 皆對應 BDD Scenario（決策與紀錄類以紀錄檔內容檢查）。檔案：`poc/AuthSpike.Tests/Features/SelectionDecisionRecord.feature`、`SelectionAndApiFirstContract.feature`、`CertificateBoundOrderCreation.feature`。

| checkbox | Scenario |
|---|---|
| 1 API First／Code First | `API 開發方式採 API First 且未引入 Code First 產生器` |
| 2 OpenIddict 候選核對 | `OpenIddict 候選為正式版本並以 ASP.NET Core 10 執行`、`OpenIddict 候選的正式版本與發佈時間有紀錄`、`OpenIddict 候選的維護狀態有紀錄`、`OpenIddict 套件授權條件為 Apache-2.0`、`OpenIddict 候選與 ASP.NET Core 10 整合` |
| 3 mTLS 憑證綁定 Opaque Token | `無法取得 Token 的呼叫端`、`綁定憑證取得不透明 Token`、`同一憑證搭配 Token 成功建立訂單`、`偷到 Token 但憑證不符無法建立訂單` |
| 4 支援證據分級 | `證據依支援等級分類`、`PKI 用戶端認證不被宣稱為已支援`、`證據不以 dev 文件作為可用性證明` |
| 5 Gateway（本 lab 不實作） | `Gateway 項目標註為本 lab 不實作且不阻擋`（紀錄）；`Gateway 終止 mTLS 並以信任契約轉送`（`@ignore`，略過） |
| 6 產品選型與示範操作確認 | `授權伺服器採使用者確認的 OpenIddict 候選`、`首個示範業務操作為建立訂單` |
| 7 可重現方式與未決事項 | `驗證方式可重現`、`已確認契約與未決事項有紀錄` |
- 未實作：Idempotency、簽章、nonce、業務去重與追查紀錄（屬 issue 02–09）。

### 產出檔案

- `poc/AuthSpike/` 授權伺服器、建立訂單 API、測試 PKI、組合根（`Hosting/SpikeRuntime.cs`）、`doc/openapi.yml`。
- `poc/AuthSpike.Tests/` Reqnroll 測試：`Features/CertificateBoundOrderCreation.feature`、`Features/SelectionAndApiFirstContract.feature`、步驟與 Hooks。

