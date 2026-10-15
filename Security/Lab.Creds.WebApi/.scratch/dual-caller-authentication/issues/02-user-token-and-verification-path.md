# 02: 使用者 Token 簽發與使用者驗證路徑

**What to build:** 授權伺服器可為測試使用者簽發使用者 Token，該 Token 帶有獨立於服務 Token 的 audience 與呼叫者類型。業務 API 對建立訂單端點接受有效的使用者 Token，通過使用者驗證後產出類型為使用者的已驗證呼叫者，業務操作以該使用者身分提交。偽造、過期或簽發者不符的使用者 Token 被拒絕。使用者路徑不要求 mTLS 綁定與請求簽章，這是設計上的差異，且不得讓服務路徑跟著放寬。本 lab 以測試使用者帳號代替真實登入流程。

**Blocked by:** 01 — 統一的已驗證呼叫者與呼叫者類型。

**Status:** resolved

- [x] 授權伺服器為測試使用者簽發使用者 Token，含獨立 audience 與呼叫者類型；使用同一個已選型的授權伺服器，不引入新產品（AC-D01）。
- [x] 業務 API 對建立訂單接受有效的使用者 Token，已驗證呼叫者類型為使用者，業務操作以該使用者身分提交（AC-D01）。
- [x] 偽造、過期或簽發者不符的使用者 Token 被拒絕，且無業務副作用（AC-D06）。
- [x] 使用者驗證涵蓋簽發者、audience、有效期、呼叫者類型與使用者有效狀態。
- [x] 使用者路徑不要求 mTLS 與請求簽章；服務路徑的保護要求不因此改變。
- [x] 使用者 Token 的效期、表示方式（audience 與呼叫者類型）為 lab 暫定值並標註「待使用者確認」。
- [x] 以真實協定與密碼驗證路徑測試，不以永遠成功的替身取代；每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作。
- [x] 互動式登入介面與使用者管理不在本票範圍。

## 實作紀錄

### 變更內容

- `poc/AuthSpike/Verification/UserCallers.cs`（新增）：使用者呼叫者的表示方式（`orders-user-client` 公開 Client、使用者 audience `orders-api-user`、`caller_type=user` claim、測試使用者帳號）。全部為 lab 暫定值。
- `poc/AuthSpike/AuthServer/AuthServerHost.cs`：
  - `/connect/token` 新增 password grant 分支，只接受使用者 Client 與測試使用者帳密；核發 `sub`=使用者、`caller_type=user`、`aud`=使用者 audience 的 Token，不要求用戶端憑證；密碼錯誤或使用者停用回 400 `invalid_grant`。
  - 新增公開 Client `orders-user-client`（僅 token endpoint 與 password 權限）與業務 API 的使用者路徑內省身分 `orders-api-user`（與資源端相同的 mTLS 憑證、僅 introspection 權限）。
- `poc/AuthSpike/Verification/UserTokenIntrospector.cs`（新增）：使用者路徑以 `orders-api-user` 身分內省 Token，檢查 `active`、簽發者（`iss`）與有效期（`exp`）；連線或回應異常一律 Unavailable（fail closed）。
- `poc/AuthSpike/Verification/CallerVerifier.cs`：依驗證路徑決定呼叫者類型。未出示用戶端憑證 → 使用者路徑（不要求 mTLS，需使用者 audience、`caller_type=user`、使用者啟用）；出示憑證 → 服務路徑（既有行為不變）。兩路徑共用查證快取。
- `poc/AuthSpike/Verification/VerifiedCaller.cs`：呼叫者類型新增 `User`，`FromUserPrincipal`。
- `poc/AuthSpike/OrdersApi/OrdersApiHost.cs`：依已驗證呼叫者類型附加服務或使用者呼叫者；使用者路徑略過請求簽章與 nonce（設計差異），仍先寫入稽核；OpenIddict validation 仍只接受服務 audience。
- `poc/AuthSpike/Trust/TrustRegistry.cs`：新增使用者停用狀態（`DisableUser`、`IsUserEnabled`），每次請求直接讀取。
- `poc/AuthSpike.Tests/Features/UserTokenVerificationPath.feature`（新增）：9 個 Scenario（AC-D01、AC-D06 與服務 Token 不能以使用者路徑通過、使用者停用）與 1 個 `@record` 情境。
- `poc/AuthSpike.Tests/Steps/UserTokenVerificationPathSteps.cs`（新增）：真實 password grant、內省、HTTP 請求與另一個環境的簽發者不符情境。

### TDD 順序與結果

- 紅燈：先寫 Feature 與步驟（僅加常數與停用狀態的最小骨架使其可編譯）。執行結果 7 個失敗（AC-D01 內省與建立訂單、錯誤密碼、過期、簽發者不符、使用者停用、`@record`）。偽造 Token 與服務 Token 走使用者路徑兩個情境在紅燈時即回應 401，作為保護基線保留。
- 綠燈：實作後使用者路徑 22 個情境通過（含上述保護基線）；`@record` 情境待本票勾選完成後轉綠。
- 過程中的修正：OpenIddict 內省的 audience 比對以呼叫端 client_id 為準，使用者 Token（aud=使用者 audience）無法由資源端服務身分內省，因此改由 `orders-api-user` 身分內省；`GetAudiences` 對手動建立的 principal 不讀取 `aud` claim，改以 claim 直接比對。

### lab 暫定、待使用者確認

- 使用者 Token 的效期沿用服務 Token 的 300 秒（lab 暫定，待使用者確認）。
- 使用者 Token 的表示方式：audience `orders-api-user`、claim `caller_type=user`、公開 Client `orders-user-client`、內省身分 `orders-api-user`（待使用者確認）。
- 測試使用者帳號與密碼為 lab 代替真實登入，不是正式登入流程；互動式登入與使用者管理不在範圍內。
- 路由規則：出示用戶端憑證即走服務路徑，不出示即走使用者路徑；使用者 Token 若附用戶端憑證會被服務路徑拒絕（AC-D04 的完整隔離與 audience 隔離收斂於 03 單）。
- 使用者寫入不做簽章與 nonce 驗證（設計差異）；防重放與冪等的具體形式於 05 單收斂。使用者路徑的稽核紀錄目前不區分呼叫者類型（07 單收斂）。
- 使用者 Token 的查證快取與撤銷同步沿用服務路徑的快取上限，使用者停用的傳播方式於 06 單收斂。

### 驗證方式（可重現）

- `dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
- 只跑本票：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "DisplayName~使用者|DisplayName~服務 Token 不能|DisplayName~02 單"`
- 完整：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
