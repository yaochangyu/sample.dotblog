# 03: 業務 API 驗證原始呼叫端請求簽章

**What to build:** 呼叫服務簽署示範業務請求，最終 API 能辨認原始呼叫端並拒絕被竄改或混用身分的請求，合法簽章請求維持正常回應。

**Blocked by:** 02 — 呼叫服務取得 Token 並經可信入口呼叫 API。

**Status:** resolved

- [x] 取得共同 HTTP Message Signatures 規則與演算法的確認，明訂必要欄位、Token 綁定資訊表示及中介改寫處理（已確認採用 RFC 9421 Lab Profile v1，規格見 `spec.md`〈請求簽章與防重放〉）。
- [x] 所有示範業務 API 呼叫必須簽章；無 Body 請求仍驗證方法、目標與必要欄位。
- [x] 有 Body 時簽章涵蓋摘要與必要內容型別，最終 API 核對摘要和收到的實際 Body 相符。
- [x] 涵蓋完整業務目標、會影響語意的查詢參數、必要標頭、適用時的 Idempotency Key 及 Token 綁定資訊。
- [x] 簽章使用不同於 mTLS 的私鑰，並確認簽章金鑰屬於已驗證 Token 識別的同一 Client。
- [x] 缺少簽章、內容或目標遭竄改、替換授權脈絡、混用不同 Client 合法金鑰時拒絕。
- [x] Gateway 不破壞簽章驗證脈絡；若有改寫，以已確認契約保留可驗證的原始資訊，不信任外部自稱的原始欄位。
- [x] 使用實際簽署與驗證的整合情境證明合法請求可通過及竄改請求被拒絕，不宣稱 nonce 防重放已完成。

對應驗收：AC-03 的簽章金鑰部分、AC-07。

### 已確認之簽章規格摘要（RFC 9421 Lab Profile v1）
- **演算法與格式**：RFC 9421 IANA `ecdsa-p256-sha256`（IEEE P1363 `r || s` 64-byte 格式，.NET 10 `ECDsa.SignData/VerifyData` 預設）；每 Client / 各環境獨立 P-256 金鑰對，與 mTLS RSA-2048 分開；`keyid` 靜態登記，且所屬 `client_id` 須與 Token 一致，禁止動態演算法協商。
- **標頭與參數**：單一標籤 `sig1`；`@signature-params` 固定順序 `;created=<int>;expires=<int>;nonce="<str>";keyid="<str>";alg="ecdsa-p256-sha256"`。
- **覆蓋元件順序**：
  - 讀取無 Body（GET/HEAD）：`"@method" "@authority" "@path" "@query" "authorization"`（無 query 時值為 `?`；直接簽署原始 `Authorization` 標頭值，禁止自訂 token-hash 標頭；日誌禁止輸出 Token、Authorization、signature base 或簽章值）。
  - 有副作用無 Body（DELETE 無 Body）：`"@method" "@authority" "@path" "@query" "authorization" "idempotency-key"`。
  - 有副作用有 Body（POST/PUT/PATCH，或 DELETE 有 Body）：`"@method" "@authority" "@path" "@query" "authorization" "content-type" "content-digest" "idempotency-key"`。
- **Body 與摘要**：依實際接收 bytes 判斷（不單靠 `Content-Length`）；有 Body 必加 `content-digest`（RFC 9530 `sha-256=:<標準 base64>:` 逐 byte 比對）與 `content-type`。
- **時間與 Nonce**：`expires = created + 60`；驗證條件 `expires > created`、`expires - created <= 60`、`created - 30 <= now <= expires + 30`（容差 30 秒，最大接受區間 120 秒）；nonce 採 CSPRNG 16 bytes 無 padding base64url（22 字元），每 attempt 新值；03 僅驗格式與時間窗，不驗唯一性（防重放由 04 實作）。
- **驗證邊界**：RFC B.2.4 測試向量與 Envoy 標頭（`Authorization`、`Content-Digest`、`Signature`、`Signature-Input`）逐字轉送已實測驗證通過（見 `tests/Lab.Creds.Proof.Tests/Unit/SignatureProfileTests.cs`、`tests/Lab.Creds.Proof.Tests/Features/GatewayEnvoy.feature`、`tests/Lab.Creds.Proof.Tests/Features/SignedRequests.feature` 與 `src/Lab.Creds.Proof/README.md` 5.2）；不宣稱已完成防重放（防重放由 Ticket 04 實作）；05 資料範圍授權來源未定。完整契約詳見 `spec.md`。

## Answer
- **結案狀態**：本 Ticket 03 所有 8 項驗收條件均已完成實作、雙軸審查修正與獨立驗證，依 issue tracker 標準正式結案（`Status: resolved`）。
- **Merge Commit Pointers**：
  - `bc9516d7` ✨ feat(api-signatures)(03): 實作 RFC 9421 業務請求簽章（已 fast-forward 合入 `integration/server-to-server-api-protection`）
  - `a7afabcd` 🔧 fix(api-signatures)(03): 正規化 authority 預設 port（已 fast-forward 合入 `integration/server-to-server-api-protection`）
- **雙軸審查證據（固定基準：`d27a2b8d`，最終 HEAD：`a7afabcd`，審查區間 `d27a2b8d...a7afabcd`）**：
  - **Standards Review**：初審 2 項 findings（OWS 超寬 trim 修正為嚴格 SP/HTAB、canonical Status 修正）均經 follow-up 複審通過；新增之 authority 預設 port 修正經 follow-up 複審為 0 breach / 0 high-confidence smell。
  - **Spec Review**：初審 2 項疑慮（lowercase derived authority、Gateway pass-through 轉送證據）經 follow-up 證實均為 false positive；新增之 authority fix 完整滿足 RFC 9421 §2.2.3 與 RFC 9110 §4.2.3 規範（hostname 轉為 lowercase、僅在為當前 scheme 之預設 port 時省略 port，Kestrel scheme 受信並完整保留 non-default port），Spec follow-up 0 finding。
- **測試驗證（共 224 測試全數通過）**：
  - **88 項 Reqnroll BDD 情境**：ClientCredentialsMtls 5 項、ProtectedApi 12 項、GatewayEnvoy 22 項、SignedRequests 49 項（涵蓋 8 個合法請求形狀、whoami、無簽章、29 個竄改/不合規範例、他 Client 金鑰拒絕、GET 帶本文拒絕、窗內重送接受、直連 API 拒絕、chunked 與空本文處理、日誌不含機密等）。
  - **136 項單元測試**：`CleanupRunnerTests.cs` 2 項、`tests/Lab.Creds.Proof.Tests/Unit/SignatureProfileTests.cs` 134 項（含 RFC 9421 B.2.4 向量互通 5 項、嚴格 Structured Field 解析 43 項、Lab Profile 驗證 86 項）。
  - 全套 224 項測試全數通過（`Build 0 Warning / 0 Error`，完整 suite 224 passed）。
- **階段限制與界線保留**：
  - **防重放未完成**：Ticket 03 僅驗證簽章格式、參數與有效時間窗（有效 60 秒、容差 ±30 秒、最大接受區間 120 秒），在 120 秒時間窗內重送同一份有效簽章仍可能被接受；**嚴禁宣稱已完成防重放**，跨實例 Nonce 去重與防重放由 Ticket 04 實作。
  - **業務去重未完成**：`idempotency-key` 僅強制簽入以防竄改，業務去重與本地提交保證由 Ticket 06 實作。
  - **業務資料授權未定**：尚未涵蓋業務資料範圍隔離（Ticket 05），Scope 僅源自 Token。
  - **其他邊界**：60 秒完整撤銷生效 SLA（Ticket 07）、金鑰輪替（Ticket 08）、生產級 Gateway HA/SDS 動態設定均保留為後續 tickets 驗收範圍。
- **DAG 解鎖通知**：
  - Ticket 03 結案後，相依之 **Ticket 04**（`Blocked by: 03`）、**Ticket 07**（`Blocked by: 03`）、**Ticket 09**（`Blocked by: 03`），連同先前已解鎖的 **Ticket 05**（`Blocked by: 02`），正式構成目前 task graph DAG 的後續 frontier（**04, 05, 07, 09**），可供主 session 接續派工。
  - Tickets 06, 08, 10 仍依 DAG 維持各自 blocked 狀態。
