# Lab.Creds.Proof 可重現驗證說明

本文件記錄 `Security/Lab.Creds.WebApi` 專案中 Ticket 01／02 之 proof 可重現環境、架構契約與驗證指令，供後續開發與審查獨立復現，不依賴本機 session 絕對路徑。

---

## 1. 環境與工具前提

- **作業系統**：Linux / WSL2 (Ubuntu 24.04 等)
- **.NET SDK**：.NET 10 SDK（`dotnet --version` >= 10.0）
- **容器環境**：Docker daemon 正常運作（具備拉取與執行 Linux 容器權限）
- **本機測試環境變數**：
  ```bash
  export TESTCONTAINERS_RYUK_DISABLED=true
  ```
  _註記：本機測試因特定 WSL2 / Docker 環境下 Testcontainers Ryuk sidecar 容器啟動或通訊限制而配置此環境變數；因原始確切限制細節未留存更深記錄，此處依實記載為本機執行參數，非通用強制要求。_

---

## 2. 精確建置與測試指令

由專案根目錄（`/mnt/d/lab/sample.dotblog/Security/Lab.Creds.WebApi`）執行：

```bash
# 建置（0 Warning / 0 Error）
dotnet build tests/Lab.Creds.Proof.Tests/Lab.Creds.Proof.Tests.csproj --nologo

# 執行驗證測試（全數 41 項測試通過）
dotnet test tests/Lab.Creds.Proof.Tests/Lab.Creds.Proof.Tests.csproj --nologo --no-build
```

---

## 3. 元件基線與版本固定

- **業務 API Gateway**：
  - 映像檔：`envoyproxy/envoy:v1.39.3@sha256:dd85940439de19a0b6ae8419610363ea0ad351d9a994ea007161c206ec1e1865`（OCI image index 多平台 manifest list）。
  - Lab 實作選型，非 production 驗收。
- **OAuth 授權核心**：
  - OpenIddict 7.7.1。
- **測試資料庫**：
  - Testcontainers 動態啟動 `postgres:16-alpine` 獨立執行個體，測試完成自動銷毀，不污染亦不共用宿主機資料庫。
- **憑證與金鑰**：
  - 所有憑證均於 runtime 動態產生（RSA-2048 自簽用戶端憑證、獨立 Gateway 憑證、非受信測試憑證），不落地亦不入版控。
  - 全程啟用 TLS/mTLS 驗證，絕不停用 TLS 檢驗。

---

## 4. 架構與接入契約

```
[Token Endpoint 路徑]
Caller ──── mTLS (RSA-2048 自簽憑證) ────> Auth Server (OpenIddict 7.7.1)
(直接直連，未經 Gateway；核發綁定呼叫端憑證之 Opaque Token)

[業務 API 呼叫路徑]
Caller ──── mTLS (RSA-2048 自簽憑證, SHA-256 allowlist) ────> Envoy Gateway (v1.39.3)
       ──── mTLS (獨立 Gateway 憑證, API SHA-256 固定 pin) ───> Business API (Kestrel)
```

1. **Caller 認證**：Envoy 設 `require_client_certificate: true` 與 `verify_certificate_hash`，不使用 `ACCEPT_UNTRUSTED`。
2. **防入口繞過**：Envoy → API 採獨立 Gateway mTLS 通道，API 以固定（pin）Gateway 憑證 SHA-256 指紋；caller 直接連線 API 或持未受信 Gateway 憑證握手即被拒絕。
3. **標頭覆寫與身分比對**：Envoy 設 `forward_client_cert_details: SANITIZE_SET` + `set_current_client_cert_details: {cert: true}` 覆寫 XFCC；API 僅在可信通道中由原始 client cert DER 計算 `x5t#S256`（SHA-256 base64url），交由 OpenIddict 與 token introspection 之 `cnf.x5t#S256` 比對；不信任任何公開標頭。
4. **原始資訊保留**：在 HTTP/1.1 設定下（`normalize_path: false`, `merge_slashes: false`, `path_with_escaped_slashes_action: KEEP_UNCHANGED`），保留 Host（不改寫）、RawTarget、query、body 與必要簽章標頭逐字保留（不宣稱 RFC 9421 驗簽）。

---

## 5. 測試組成（共 41 測試）

測試套件共包含 **41 項測試**（Ticket 01 基線 29 項 + Ticket 02 新增 12 項；非全為 BDD）：
- **39 項 Reqnroll BDD 情境**（5 + 12 + 22 = 39）：
  - 5 項 Client Credentials mTLS 情境（`ClientCredentialsMtls.feature`，含 Ticket 02 新增的 5 分鐘效期）
  - 12 項受保護 API 情境（`ProtectedApi.feature`，含 Ticket 02 新增的過期後重新取 token、無效 token、錯誤目標 API、無 client_secret 入口）
  - 22 項 Gateway Envoy 情境（`GatewayEnvoy.feature`：10 個一般情境 + 12 個 XFCC 負面 Scenario Outline 範例；含 Ticket 02 新增的 6 個嚴格 XFCC 範例與 Body／標頭身分宣稱情境）
  - 歷史基線（Ticket 01）為 29 項 = 27 BDD + 2 單元；Ticket 02 新增 12 項 BDD，現況合計 41 項。
- **2 項 CleanupRunner 單元測試**：
  - `CleanupRunnerTests.cs`（驗證受控資源清理在步驟拋出例外時仍依序執行後續清理，並完整保留原始例外與堆疊）
- **程式碼生成**：Reqnroll 生成檔案置於 `obj/`（設定 `ReqnrollUseIntermediateOutputPathForCodeBehind=true`），不追蹤 `.feature.cs` 程式碼。

---

## 5.1 Ticket 02 契約與決策

- **Token 效期**：Lab Opaque Access Token 有效期 5 分鐘（`ProofDefaults.AccessTokenLifetime`）。到期後重新以 Client Credentials（mTLS）取得；不使用 Refresh Token；不以自然到期取代 60 秒撤銷要求（撤銷屬後續 ticket）。
- **效期驗證**：以真實協定驗證 `expires_in` 介於 299–300 秒、introspection `exp − iat` 恰為 300 秒；過期以將該 token 的資料庫 `ExpirationDate` 改為過去來驗證（不 sleep 5 分鐘、不縮短 production TTL），並驗證過期 token 被拒絕、重新取得後成功。
- **憑證入口**：每個示範 Client 有獨立 RSA-2048 自簽憑證；無 API Key / Client Secret 替代入口（只帶 `client_secret` 不能取得 token）。
- **目標 API**：token audience 為 `partner-api`；以 audience 為 `inventory-api` 的另一個 API 呼叫會被拒絕。
- **XFCC 嚴格格式**：僅接受 Envoy v1.39.3 實際輸出的單一元素 `Hash=<sha256 hex>;Cert="<URL-encoded PEM>"`；Hash 必須等於憑證 SHA-256，Cert 須嚴格百分比編碼並解出恰一張 PEM 憑證；其餘（缺 Hash、Hash 不符、附加欄位、多張憑證、非法編碼、重複標頭等）一律 401。
- **階段限制**：尚無 HTTP Message Signatures、nonce/防重放、Idempotency、完整業務授權、60 秒撤銷 SLA、HA/SDS、憑證輪替。

## 6. 限制、未驗項目與歷史誠實性

1. **Proof 專用端點**：`/partner/inspect/{**rest}` 為 proof 診斷端點，用以驗證轉送指紋與原始請求欄位，不屬於正式開放合約（未列於 `doc/openapi.yml`）。
2. **XFCC 解析器**：已改為針對 Envoy v1.39.3 實際格式的嚴格單一元素解析（見 5.1）；僅適用於 Envoy `SANITIZE_SET` + `cert: true` 的輸出，不是完整 XFCC grammar。
3. **生產級能力未定**：Gateway 高可用（HA）、動態 xDS/SDS、動態憑證輪替等生產環境配置未驗證。
4. **後續 Tickets 驗收項目**：60 秒完整撤銷（含長連線）、服務故障 fail-closed、HTTP Message Signatures 驗簽/nonce/去重/輪替皆為後續票（03–10）範圍。
5. **歷史紀錄誠信**：S2、G3 及前四項 XFCC 補測非 red-first（G3 當時已有 SANITIZE_SET，補測以變異測試驗證敏感度；前四項 XFCC 補測因既有程式碼已通過故為 green-first），如實記錄不虛構 red 歷程。
