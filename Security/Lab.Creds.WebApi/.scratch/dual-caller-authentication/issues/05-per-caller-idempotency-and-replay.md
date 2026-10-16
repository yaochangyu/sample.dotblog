# 05: 冪等與防重放依呼叫者身分分開

**What to build:** 兩種呼叫者共用同一份業務邏輯與本地提交保證，但冪等去重與穩定業務識別依呼叫者類型與已驗證身分分開計算：同一業務識別由使用者與服務各自提交，互不覆蓋也不誤判為重複；使用者重試與併發提交同一業務操作時，本地副作用只提交一次。使用者寫入沒有請求簽章，因此仍需要防重放保護，不能因為缺簽章而讓寫入操作失去重放與冪等防護。

**Blocked by:** 03 — 呼叫者類型與 audience 隔離（禁止混用）。

**Status:** resolved

- [x] 同一業務識別由使用者與服務各自提交時，各自獨立，不互相覆蓋或誤判重複（AC-D09）。
- [x] 不同使用者提交相同識別時互不衝突，同一使用者範圍內業務識別唯一。
- [x] 使用者重試或併發提交同一業務操作時，本地副作用只提交一次，回傳既有結果或處理狀態（AC-D10）。
- [x] 使用者寫入請求有防重放保護；具體機制為 lab 暫定值並標註「待使用者確認」，spec 未定義不自行視為已核准。
- [x] 服務路徑既有的 nonce、冪等與業務唯一性行為不變。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作。

## 實作紀錄

### 實作內容

- 業務範圍鍵：`VerifiedCaller.BusinessScope`。服務沿用 client_id（既有行為與既有持久資料不變）；使用者為 `user:{sub}`。建立訂單的業務識別（orderReference）、Idempotency-Key 與訂單歸屬皆以此分開計算，回應中的 clientId 仍為已驗證身分。
- 使用者寫入防重放：使用者寫入（非 GET）沒有請求簽章，改以 `X-Request-Nonce` 標頭防重放，nonce 依使用者業務範圍分開保存於既有 `INonceReplayStore`（記憶體與 Redis 皆適用）。缺少或重放回應 401 且無業務副作用；防重放狀態無法讀寫回應 503（replay_state_unavailable）。
- 服務路徑（簽章、nonce、業務去重）程式未改動；`/orders` 的服務呼叫既有情境與 `@isolated` 冪等／防重放情境全數通過。
- 測試：新增 `Features/PerCallerIdempotencyAndReplay.feature` 與對應步驟；先寫 Scenario 並確認紅燈（業務數 0、重放回 201），再實作轉綠。既有使用者路徑與 04 單測試步驟補上 `X-Request-Nonce`，斷言強度未降低（04 單的使用者業務數步驟改以使用者業務範圍計數）。

### lab 暫定、待使用者確認

- 使用者防重放機制（`X-Request-Nonce` 標頭、nonce 上限 128 字元、保存期 24 小時）為 lab 暫定值，待使用者確認；spec 未定義不視為已核准。
- 使用者寫入重試須以新的 nonce 重送，並沿用同一 Idempotency-Key 與 orderReference；業務去重仍為 lab 暫定（沿用 06 單）。
- 使用者業務範圍鍵的前綴格式 `user:` 為 lab 暫定值，待使用者確認。
- 使用者 Token 的查證快取與撤銷同步沿用 02／07 單既有 lab 值，本票未調整。

### 驗證方式（可重現）

- 建置：`dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
- 本票行為：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "DisplayName~AC-D09|DisplayName~AC-D10|DisplayName~05 單|DisplayName~不同使用者提交相同業務識別|DisplayName~使用者寫入|DisplayName~服務路徑既有的 nonce"`
- 受影響既有範圍：`Category=user-caller|Category=caller-authorization`、`Category=caller-isolation|Category=unified-caller|Category=audit`，以及 IdempotentBusinessCommit、ReplayProtectionAndRetry、RevocationAndFailClosed、RejectedRequestAudit 的 `@isolated` 情境。
- 完整測試套件由主 session 於合併後執行（本票未執行完整套件）。

