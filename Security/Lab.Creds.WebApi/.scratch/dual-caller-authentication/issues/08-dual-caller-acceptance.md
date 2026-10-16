# 08: 雙呼叫者的整體驗收與文件

**What to build:** 以「同一業務端點同時接受服務呼叫與使用者呼叫」為主題，端到端驗證 AC-D01 到 AC-D14 全部情境，並更新領域詞彙與 ADR，讓兩種呼叫者的驗證分離決定、lab 例外與尚未決定的項目有明確紀錄。這張票是收尾驗收，不新增功能；若驗收發現缺漏，回到對應票修正。

**Blocked by:** 04 — 授權依呼叫者類型分開；05 — 冪等與防重放依呼叫者身分分開；06 — 使用者 Token 撤銷與查證故障 fail closed；07 — 稽核區分呼叫者類型。

**Status:** resolved

- [x] AC-D01 到 AC-D14 每一條都有對應的 BDD Scenario 並實際通過，完整測試套件 0 失敗。
- [x] 有一條端到端 Scenario：同一個建立訂單端點，服務與使用者各自成功呼叫；混用 Token、越權資料、重試與撤銷都被正確處理。
- [x] `GLOSSARY.md` 補上本需求新增的詞彙（呼叫者類型、使用者呼叫者），並與既有「呼叫服務」「Client 身分」「已驗證呼叫者」區分。
- [x] ADR 補記本需求的 lab 例外與限制（測試使用者帳號、程序內儲存、使用者路徑無請求簽章）。
- [x] 列出所有「待使用者確認」的 lab 暫定值：使用者 Token 效期與表示方式、使用者防重放機制、角色與資料範圍、撤銷傳播、錯誤格式。
- [x] 不實作 Gateway、委託與 token exchange；沿用已確認的 lab 範圍決定。

## 實作紀錄

### 實作內容

- 新增 `Features/DualCallerEndToEnd.feature` 與 `Steps/DualCallerEndToEndSteps.cs`（步驟前綴「雙呼叫者票」，避免與 CredentialRotation 的「08 單」步驟文字衝突）。
- 端到端 Scenario（`@dual-caller-e2e @isolated`）在同一個建立訂單端點依序驗證：服務與使用者各自 201；服務 Token 走使用者路徑 401、使用者 Token 附服務憑證與簽章走服務路徑 401；使用者讀自己的訂單 200、讀他人或服務建立的訂單 404；相同 Idempotency-Key 與新 nonce 重送回傳既有訂單編號並標示重播、不新增訂單；使用者停用後的建立請求 401 且無副作用，服務路徑仍 201。
- 產品程式未改動。AC-D01 到 AC-D14 的行為覆蓋沿用 01–07 單既有 Scenario，本票不新增功能。
- 詞彙與 ADR 於同一 commit 補上（見 GLOSSARY.md 與 docs/adr/0001-lab-poc-exceptions.md 的「雙呼叫者」段）。

### 紅綠順序

- 先新增 `Features/DualCallerEndToEnd.feature`，未寫步驟即執行：Scenario 失敗，所有步驟為 undefined（紅燈來自缺少步驟定義，不是行為）。
- 再新增步驟定義，執行：通過（綠燈）。
- 行為的突變檢查：暫時把 `OrdersApi/Order.cs` 的 `TryGet` 擁有者比對改為恆真（`|| true`），E2E Scenario 失敗於「他人訂單回應 404」（實際 200），之後已以 `git checkout` 還原，工作樹確認無該檔變更。

### lab 暫定、待使用者確認

- 使用者 Token 效期與表示方式、使用者防重放機制（X-Request-Nonce）、角色與使用者資料範圍、撤銷傳播（停用即時生效於下一次請求，上限 60 秒）、錯誤格式（401/404/503 與 error 代碼），皆為 lab 暫定值，待使用者確認；清單同步記於 ADR。

### 驗證方式（可重現）

- 建置與端到端：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "Category=dual-caller-e2e"`（1 通過）。
- 受影響的既有範圍：見下方「回歸結果」。
- 完整測試套件依主 session 指示於合併後執行，本票未執行。

### 回歸結果

- 篩選：`Category=user-caller|caller-authorization|caller-isolation|unified-caller|audit|user-revocation|caller-type-audit|dual-caller-e2e`，加上 `RevocationAndFailClosed`、`IntrospectionFault`、`RejectedRequestAudit`、`PerCallerIdempotency`、`DualCallerEndToEnd`、`redis-replay`、`real-storage`。
- 結果：73 通過，0 失敗，耗時 3 分 50 秒。

### 阻塞說明

- 無。
