# 24: Redis 防重放與防重放紀元

**What to build:** 請求簽章的 nonce 防重放改由 Redis 提供，登錄是原子的，並在多個業務 API 執行個體之間一致。Redis 紀錄遺失（重啟或被清空）時，防重放紀元重設為當下，簽章建立時間早於紀元的請求一律被拒絕；呼叫端用新 nonce 與新時間重簽即可通過。Redis 不可用時 Fail Closed。

**Blocked by:** 22 — 真實儲存的測試基礎與故障注入。

**Status:** closed

- [x] 同一份簽章併發送往兩個業務 API 執行個體，只有一個通過防重放，另一個被拒絕且無業務副作用（AC-31，強化 AC-08）。
- [x] Redis 被清空或重啟後，重放遺失前建立的簽章被拒絕且無業務副作用；紀元之後建立的簽章照常通過（AC-32）。
- [x] 被紀元拒絕的呼叫端以新 nonce 與新時間重簽後可以通過。
- [x] Redis 不可用時請求被拒絕並明確回報服務錯誤，無業務副作用（AC-33，沿用 AC-05 的 Fail Closed）。
- [x] nonce 保存期涵蓋簽章接受期限加時鐘容差；合法重試重簽換新 nonce 仍可通過（沿用既有簽章與防重放 Scenario）。
- [x] nonce 登錄後若後續業務提交失敗，不回收 nonce；同一份簽章重送仍被擋，合法重試必須重簽。
- [x] 先驗證簽章、摘要與時間，通過後才登錄 nonce，未驗證的請求不污染 nonce 紀錄。
- [x] 現有簽章與防重放的 Scenario 全部維持通過，斷言不放寬。
- [x] Redis 是否啟用持久化屬未定營運設定，本票不自行決定，紀錄為待確認。
- [x] 不實作 Gateway 相關項目；沿用已確認的 lab 範圍決定。

## 實作與審查紀錄

- **實作重點**：
  - 引入 `StackExchange.Redis` (3.4.0)，建立 `INonceReplayStore` 介面與 `ReplayRegistration` 列舉（`Registered`, `Replayed`, `BeforeReplayEpoch`）。
  - `RedisNonceReplayStore` 透過單一原子 Lua 腳本處理紀元檢查、動態初始化與 `SET ... EX ttl NX`。
  - 當 Redis 清空後首個請求到達時，動態初始化 `epoch_sec = now_sec`，並以 `created_sec <= epoch_sec` 阻擋遺失前同秒（及更早）簽署之舊簽章重放（回傳 401 `signature_before_replay_epoch`）。
  - 系統正常連線啟動時，`EnsureEpochInitializedAsync` 將紀元初始化為 `now_sec - 1`，確保當前秒合法新簽章可通過。
  - 遇到連線或逾時異常時拋出 `ReplayStateUnavailableException`，API 回報 503 `replay_state_unavailable`。
  - 雙執行個體 BDD 測試（`RedisReplayProtectionAndEpoch.feature`）涵蓋併發去重、清空後早於紀元拒絕、重簽通過與 Redis 不可用 503。
- **審查結論**：Copilot CLI (`gpt-5.5`) 雙軸審查 Verdict：Pass（Standards: Pass, Spec: Pass）。
- **待確認營運設定**：Redis 持久化（RDB / AOF）設定依部署環境營運規範決定，本票維持待確認紀錄。
