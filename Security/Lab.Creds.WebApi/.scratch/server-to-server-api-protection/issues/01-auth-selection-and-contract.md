# 01: 確認 auth 技術選型與接入契約

**What to build:** 以最小可執行驗證確認授權伺服器與可信入口的候選方案能提供既定接入保證，將證據與必要契約交給使用者決策，解除後續實作阻擋。本單是研究／決策前置單，不代表正式業務功能完成。

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [x] 閱讀已指定的 api.template 開發規則及其必讀指南，確認全專案採 API First 或 Code First，不自行假設。（已確認全專案採 API First，最小 OpenAPI，不做 codegen）
- [x] 核對授權核心產品之正式版本、授權條件、維護狀態及與 ASP.NET Core 10 整合方式。（使用者已確認選定 OpenIddict 7.7.1 作為 OAuth 授權核心）
- [ ] 以真實協定及密碼驗證路徑展示 mTLS Client Credentials 核發憑證綁定 Opaque Token、introspection 與 API 憑證綁定判斷；無憑證及不同憑證呼叫失敗。（Lab 第一版暫以已驗證 RSA-2048 自簽 client cert 為接入範圍；其他憑證類型保留未驗）
- [ ] 證據區分正式版本支援、需整合功能與尚未支援能力，不以 dev 文件或永遠成功的替身作為可用性證明。
- [ ] 確認 Gateway 候選與信任契約，涵蓋下游通道認證、防入口繞過、驗證資訊來源與原始簽章內容保留。（Lab 實作基線選定 Envoy v1.39.3，最小可信邊界已由 proof 驗證；但正式 code-review 未完成、proof 未提交、生產級設定未定，本項維持未勾選）
- [ ] 取得使用者對產品選型、API 開發方式及首個示範業務操作的確認；缺乏可行方案時明確列出阻擋，不降級既定保護。
- [ ] 記錄可重現驗證方式、已確認契約與未決事項；不手刻 OAuth 協定作為未經核准的替代品。

## 已確認決策與邊界紀錄
- **OAuth 授權核心**：使用者已確認採用 OpenIddict 7.7.1。
- **開發模式**：全專案採 API First（最小 OpenAPI 規格，不做 codegen）。
- **Client 憑證接入範圍**：Lab 第一版暫以已驗證 RSA-2048 自簽 client cert 為接入範圍；其他憑證類型（ECDSA、PKI 階層式鏈）保留未驗，避免將候選或 proof 誤寫成完整功能。
- **簽章演算法獨立性**：嚴格區分 mTLS RSA 用戶端憑證與 HTTP Message Signatures 演算法，HTTP Message Signatures 簽章演算法與參數契約目前未定。
- **資料與測試堆疊**：EF Core + PostgreSQL、隔離 Testcontainers、Reqnroll BDD（生成物於 `obj/`，不追蹤 `.feature.cs`）。
- **角色權責**：主 session 為使用者明確批准之 merger，負責 merge/commit/push，其他工作維持專責派工。
- **Lab Gateway 選型與最小可信邊界**：
  - 使用者已確認 Lab 實作基線採用 Envoy v1.39.3（固定 digest `envoyproxy/envoy:v1.39.3@sha256:dd85940439de19a0b6ae8419610363ea0ad351d9a994ea007161c206ec1e1865`）作業務 API Gateway；此為 Lab 選型，不等於 production 驗收。
  - Token 端點接入路徑：呼叫端（caller）直接以 mTLS 連線授權伺服器（不經 Gateway）。
  - Caller 認證：Envoy 採 caller RSA-2048 自簽憑證 SHA-256 allowlist（`verify_certificate_hash`，不設 `ACCEPT_UNTRUSTED`）。
  - 下游通道認證與防繞過：Envoy → API 採獨立 Gateway mTLS 通道，API 以 SHA-256 固定（pin）特定 Gateway 憑證身分，防範 caller 直接繞過 Gateway 或非受信 Gateway 呼叫。
  - 標頭覆寫與防偽造：Envoy 設 `forward_client_cert_details: SANITIZE_SET` + `set_current_client_cert_details: {cert: true}` 覆寫 XFCC 標頭，阻擋外部偽造。業務 API 不得信任任何其他轉送身分標頭。
  - 身分比對：API 僅在可信下游通道中由原始 client cert DER 計算 SHA-256 指紋（`x5t#S256` base64url），交由 OpenIddict 7.7.1 與 token introspection 之 `cnf.x5t#S256` 比對。
  - 原始資訊保留：在 HTTP/1.1 設定下保留 Host（不改寫）、原始 target（RawTarget）、query、body 及必要簽章標頭逐字保留，不宣稱 RFC 9421 驗簽。
- **XFCC 解析器與邊界限制**：XFCC 缺少標頭、非 PEM、損毀 PEM、缺少 Cert 欄位、重複標頭、多個 Cert 項目皆實測回傳 401（無身分洩漏）；目前解析器為 proof 級 regex，非完整 grammar 解析，正式實作須保留嚴格解析不變式（拒絕多 Cert/重複標頭/非 PEM，禁止 first-wins）。
- **Proof 驗證與未結案狀態說明**：
  - Proof worktree 27 項獨立情境已通過測試，作為 proof 證據記錄；但不將 session 絕對路徑作為唯一可重現 repo 文件。
  - 正式 fixed-baseline / code-review 尚未完成、proof 尚未提交，因此 Ticket 01 保持未結案（`Status: ready-for-agent`），後續 tickets (02-10) 保持鎖定，不提前解鎖。
- **未驗證與後續 Tickets 驗收項目**：
  - 60 秒完整撤銷生效機制（含現有長連線，SLA 待驗收）、服務故障 fail-closed、HTTP Message Signatures 驗簽/nonce/業務去重/憑證輪替皆保留為後續 tickets 驗收；不新增 HA/SDS/其他 scope 或重開架構。
  - 歷史誠信：S2、G3 及前四項 XFCC 補測非 red-first，嚴禁補造歷史。
