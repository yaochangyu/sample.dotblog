# 02: 呼叫服務取得 Token 並經可信入口呼叫 API

**What to build:** 呼叫服務以獨立 Client 身分取得 Token，經可信 Gateway 呼叫 ASP.NET Core 10 示範 API；合法服務可取得回應，沒有對應憑證的呼叫不能使用 Token。本切片僅供受控測試環境，尚未完成全部請求保護。

**Blocked by:** 01 — 確認 auth 技術選型與接入契約。

**Status:** resolved

- [x] 採用 01 核准方案與開發方式，建立呼叫端 → 可信 Gateway → 業務 API 的主要整合測試邊界。（`ProofEnvironment.cs:47-94` 隔離啟動 Testcontainers Postgres、Kestrel API、Envoy Gateway，`doc/openapi.yml:8-44` 同步最小 OpenAPI 契約）
- [x] 每個示範呼叫服務、環境使用獨立 Client 與憑證；不以 API Key 或 Client Secret 提供替代認證入口。（`ClientSeeder.cs:37-45` 獨立註冊 partner-a/b 與 api-service，`TestCertificates.cs` 動態生成 RSA-2048 自簽憑證，`ProtectedApi.feature:11-25` 驗證無 client_secret 替代入口）
- [x] mTLS 用戶端認證成功才核發綁定該憑證的短效 Opaque Token；Token 效期已確認為 5 分鐘（使用者決策：到期後重新以 Client Credentials 取得，不使用 Refresh Token，不取代 60 秒撤銷要求）。（`ProofOptions.cs:17-23` `ProofDefaults.AccessTokenLifetime` 設定 5 分鐘，`ClientCredentialsMtls.feature:28-34` 驗證 expires_in 299–300s 與 exp-iat=300，`IntrospectionSteps.cs:43-56`）
- [x] Token 到期後可重新取得；缺少或無效憑證不能取得 Token。（`ProtectedApi.feature:26-38` 透過資料庫 `ExpirationDate` 狀態模擬過期並驗證 401 拒絕與重新取得成功；`ClientCredentialsMtls.feature:8-20` 驗證無憑證或 stranger 憑證拒發）
- [x] 經受保護且已認證的 introspection 介面判斷 Token 有效性、有效期限、目標 API 與憑證綁定，不只看 active。（`IntrospectionSteps.cs:36-57` 核對 active、exp、aud 與 cnf.x5t#S256 呼叫端憑證指紋，`ClientCredentialsMtls.feature:21-34`）
- [x] 只持有 Token、使用不同 Client 的憑證、錯誤目標 API 或無效 Token 時拒絕呼叫。（`ProtectedApi.feature:39-68` 驗證 token-only 遭拒、他人憑證遭 401、錯 aud 遭拒、無效 token 遭拒，`ApiSteps.cs:112-161`）
- [x] Gateway 下游通道經過認證，外部偽造的驗證資訊被移除或覆寫，業務 API 不能繞過入口。（`GatewayClientCertificateMiddleware.cs:7-53` 實作嚴格 XFCC profile 拒絕重複/多 Cert/非 PEM/損毀 PEM，`ProofHosts.cs:117-161` API 固定 Gateway 憑證 SHA-256 指紋拒絕直連或 rogue gateway，`GatewayEnvoy.feature:52-75`）
- [x] 合法呼叫建立已驗證 Client 身分，不採信 Body 或外部標頭自行宣稱的身分。（`ProofHosts.cs:108-131` 身分完全由 introspection 與憑證綁定確立，不採信 body 宣稱 partnerName 或外部偽造標頭，`GatewayEnvoy.feature:6-30`，`GatewaySteps.cs:231-254`）
- [x] 示範流程與測試可重現，明確說明此階段尚未具備簽章、防重放及完整業務授權。（`src/Lab.Creds.Proof/README.md` 完整記錄可重現建置與測試指令，全套 41 測試即 39 BDD + 2 CleanupRunner 單元測試全數通過）

**階段限制**：本切片僅示範受控環境的憑證綁定 Token 與可信入口；尚無 HTTP Message Signatures、nonce/防重放、Idempotency、完整業務授權、60 秒撤銷 SLA、HA/SDS 與憑證輪替（後續 tickets）。`/partner/inspect` 為 proof 診斷端點。

對應驗收：AC-01、AC-02、AC-03 的 Token／憑證部分、AC-06。

## Answer
- **結案狀態**：本 Ticket 02 所有 9 項驗收條件均已達成實證與雙軸審查，依 issue tracker 標準正式結案（`Status: resolved`）。
- **Merge Commit Pointer**：
  - `0c2c2a9a` ✨ feat(creds)(02): 完成短效憑證綁定 Token 呼叫切片（已 fast-forward 合入 `integration/server-to-server-api-protection`）
- **雙軸審查證據（Fixed Diff：`dbcacf17...0c2c2a9a`）**：
  - **Standards Review**：0 documented-standard breach，0 heuristic smell（EF Core IDbContextFactory、CancellationToken 傳遞、API First OpenAPI、Testcontainers 邊界均維持規範；不重開已接受之樣板抗辯）。
  - **Spec Review**：0 finding（符合受控環境憑證綁定與可信入口範圍，5 分鐘效期與 OpenAPI 契約一致，未越界倒灌後續需求）。
- **測試驗證（共 41 測試）**：
  - 39 項 Reqnroll BDD 情境（ClientCredentialsMtls 5 項、ProtectedApi 12 項、GatewayEnvoy 22 項）+ 2 項 CleanupRunner 單元測試，共 41 項測試全數通過（`0 Warning / 0 Error`）。
  - Token 效期驗證採 5 分鐘，過期驗證以資料庫 `ExpirationDate` 狀態切換驗證而非等待自然 clock；嚴格 Envoy XFCC profile（只接受單一 DER SHA-256 Hash 與單一 URL-encoded PEM Cert）。
- **階段限制與界線保留**：
  - 未驗 HTTP Message Signatures 簽章（Ticket 03）、防重放 nonce（Ticket 04）、業務資料範圍授權（Ticket 05）、業務冪等性（Ticket 06）、60 秒完整撤銷 SLA（Ticket 07）、金鑰輪替（Ticket 08）、生產級 HA/SDS。
  - `/partner/inspect` 維持 proof-only 診斷端點。
- **DAG 解鎖通知**：
  - Ticket 02 結案後，相依之 **Ticket 03**（`Blocked by: 02`）與 **Ticket 05**（`Blocked by: 02`）依 task graph DAG 同時解鎖，成為目前可供派工之 frontier。
  - Tickets 04, 06–10 仍依 DAG 維持各自 blocked 狀態。
