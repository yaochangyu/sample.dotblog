## Implementation baseline

本專案採 ASP.NET Core 10（.NET 10）。實作遵循 [api.template 的 CLAUDE.md](https://github.com/yaochangyu/api.template/blob/main/CLAUDE.md)，並於實作前閱讀其中指定的 `.claude/development-rules.md`、`.claude/decision-framework.md` 及工作項相關指南。

### 已確認選型與技術基線
- **API 開發模式**：全專案採 API First，採最小 OpenAPI 規格，不做 codegen。
- **OAuth 授權核心**：使用者已確認選定 OpenIddict 7.7.1 作為 OAuth 授權核心。
- **Client 憑證接入範圍**：Lab 第一版暫以已驗證之 RSA-2048 自簽用戶端憑證（client cert）為接入範圍；其他憑證類型（如 ECDSA、PKI 階層鏈）保留未驗，避免將候選或 proof 誤記為完整功能。
- **HTTP Message Signatures 契約基線（Ticket 03 Lab Profile v1）**：
  - 演算法與金鑰：採用 RFC 9421 IANA 註冊之 `ecdsa-p256-sha256`（§3.3.4），輸出格式為 IEEE P1363 `r || s`（64 bytes）。每個 Client 及各環境具備獨立 NIST P-256 簽章金鑰對，與 mTLS RSA-2048 用戶端憑證嚴格分開；`keyid` 靜態登記綁定 Client 身分，且所屬 `client_id` 必須與 Token introspection 之 `client_id` 一致，禁止動態演算法協商。
  - 簽章結構：限定單一標籤 `sig1`，`Signature-Input` 與 `Signature` 標頭皆為單一成員；`@signature-params` 固定參數順序為 `created`、`expires`、`nonce`、`keyid`、`alg`。
  - 基礎覆蓋元件順序：固定為 `@method`、`@authority`、`@path`、`@query`、`authorization`；`@query` 包含完整原始字串（無 query 時值為單獨 `?`，不做 percent-decode 或重排）。Token 綁定直接簽署原始 `authorization` 標頭值（禁止自訂 token hash 標頭）；日誌嚴格禁止輸出原始 Token、`Authorization` 標頭值、signature base 或簽章值。
  - Body 與摘要：依實際接收 bytes 判斷 Body（不單依賴 `Content-Length`）；有 Body 時必加 `content-type` 與 `content-digest`（RFC 9530 SHA-256 Structured Field dictionary `:base64:`，逐 byte 核對不重序列化 JSON）。GET/HEAD 在本 Lab 無 Body；DELETE 若有 Body 須依規格加摘要；無 Body 請求不得列入 content-type / content-digest。
  - 冪等鍵與時間窗：副作用方法（POST/PUT/PATCH/DELETE）必填並簽入 `idempotency-key`（Ticket 03 僅簽入，不宣稱去重完成）。簽章時間為 `expires = created + 60`；驗證端時鐘容差 ±30 秒（`expires > created`、`expires - created <= 60`、`created - 30 <= now <= expires + 30`，最大接受區間 120 秒）。`nonce` 採 CSPRNG 16 bytes 無 padding base64url（22 字元），每次嘗試新值；Ticket 03 僅驗證格式與時間窗，跨實例防重放與唯一性由 Ticket 04 實作。
  - 邊界與驗證界線：RFC 9421 B.2.4 互通測試向量與 Envoy 標頭（`Authorization`、`Content-Digest`、`Signature*`）逐字保留待 Ticket 03 實測驗收；授權範圍（scope）僅源自 Token，Ticket 05 業務授權來源與模型未定（不帶入自訂 recordRange/writePolicy）；既有 5 分鐘 Token、Envoy v1.39.3、OpenIddict 7.7.1 基線維持不變。
- **資料儲存與測試**：資料存取採 EF Core + PostgreSQL，測試邊界採隔離之 Testcontainers，規格驗收採 Reqnroll BDD（程式碼生成置於 `obj/`，不追蹤 `.feature.cs`）。
- **Lab Gateway 選型與最小可信邊界**：
  - 使用者已確認 Lab 實作基線採用 Envoy v1.39.3（固定 digest `envoyproxy/envoy:v1.39.3@sha256:dd85940439de19a0b6ae8419610363ea0ad351d9a994ea007161c206ec1e1865`）作業務 API Gateway；此為 Lab 選型，不等於 production 驗收。
  - Token endpoint 接入路徑：呼叫端（caller）直接以 mTLS 連線授權伺服器（不經 Gateway）。
  - Caller 認證：Envoy 採 caller RSA-2048 自簽憑證 SHA-256 allowlist（`verify_certificate_hash`，不設 `ACCEPT_UNTRUSTED`）。
  - 下游通道認證與防繞過：Envoy → API 採獨立 Gateway mTLS 通道，API 以 SHA-256 固定（pin）特定 Gateway 憑證身分，防範 caller 直接繞過 Gateway。
  - 憑證轉送與覆寫：Envoy 設定 `forward_client_cert_details: SANITIZE_SET` + `set_current_client_cert_details: {cert: true}` 覆寫 XFCC 標頭，阻擋外部偽造。
  - 身分比對：API 僅在可信下游通道中由原始 client cert DER 計算 SHA-256 指紋（`x5t#S256` base64url），交由 OpenIddict 7.7.1 與 token introspection 之 `cnf.x5t#S256` 比對；不信任任何其他公開身分標頭。
  - 原始資訊保留：在 HTTP/1.1 設定下保留 Host（不改寫）、原始 target（RawTarget）、query、body 及必要簽章標頭逐字保留，不宣稱 RFC 9421 驗簽。
- **XFCC 解析器與邊界限制**：XFCC 缺少、非 PEM、損毀 PEM、缺 Cert 欄位、重複標頭、多個 Cert 項目皆實測回傳 401；目前解析器屬 proof 級（非完整 grammar 解析），正式實作須保留嚴格解析不變式（拒絕多 Cert/重複標頭/非 PEM，禁止 first-wins）。
- **Proof 審查與驗證界線**：Ticket 01 proof 程式碼與固定基準雙軸審查修正已由主 session 合入 integration branch（commit `44c5fb97`, `fe77ee38`, `9c66b873`）。固定基準 review 結論中 Standards 兩項硬規範（`IDbContextFactory`、CancellationToken 傳遞）已修正並經獨立驗收（含 CleanupRunner 例外保留驗證），heuristic 程式碼樣板抗辯被接受，Spec 軸 0 finding；全套共 29 項測試（27 項 BDD + 2 項單元測試）通過（詳見 `src/Lab.Creds.Proof/README.md`）。ECDSA、PKI 階層鏈、憑證輪替、60 秒完整撤銷生效（含現有連線）、服務故障 fail-closed、HTTP Message Signatures/nonce/idempotency 皆保留為後續 tickets 驗收；S2、G3 及前四項 XFCC 補測非 red-first，嚴禁補造歷史。



### 實作與程式碼審查規範
- **角色權責**：主 session 為使用者明確批准之 merger，專責負責 merge、git commit 與 git push；所有其他研究、實作、測試及審查工作仍依既有角色派工，各 worker 不得自行提交推送、關閉分頁或自啟工作。
- **實作流程（使用 `/implement-spec`）**：依提供之流程上下文執行（非依賴技能註冊表）。
  - **分支與工作區**：整個 spec 於單一 integration branch 完成。各 implementer 於各自的 worktree / branch 作業，開始前確認基準基於 integration branch，作業完成前合入 integration 最新 tip。
  - **任務圖結構**：Tickets 規劃為具備 blocking edges 的 task graph，依相依關係推進。
  - **開發與驗證**：實作採用 TDD。各完成切片經審查後再整合回 integration branch。
  - **收尾與結案**：全部完成後執行整體 code-review、修正、依本地 tracker 結案，並清理各 implementer worktrees。
  - **職責與約束**：專案既有五個角色及主 session 負責 git commit/push 之權責保持不變。不自動啟動 implement-spec，不自行提交推送，不清理待命 tabs。若派工角色未涵蓋之事項由主 session 詢問使用者決定，不得自行指派新角色。
- **程式碼審查（使用 `/code-review`）**：
  - 正式審查前必須先固定比較基準（commit、branch 或 tag）。
  - 審查嚴格區分 **Standards**（編碼標準與規範符合度）與 **Spec**（需求規格與 issue 一致性）雙軸並行評估。



## Agent skills

以下設定只適用於 `Security/Lab.Creds.WebApi`；所有路徑均相對於此專案目錄。

### Issue tracker

Issues 與 specs 使用本地 Markdown，存於 `.scratch/<feature>/`。見 `docs/agents/issue-tracker.md`。

### Triage labels

採用五個預設 triage 角色，以 `Status:` 記錄。見 `docs/agents/triage-labels.md`。

### Domain docs

採 single-context：本專案目錄的 `GLOSSARY.md` 與 `docs/adr/`。見 `docs/agents/domain.md`。
