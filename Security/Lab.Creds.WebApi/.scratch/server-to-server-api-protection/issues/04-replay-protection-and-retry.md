# 04: 跨執行個體防重放並支援合法重簽重試

**What to build:** 同一份簽章即使併發送往不同 API 執行個體，也不會被重複接受；呼叫服務可以使用新 nonce 與簽章進行合法重試。

**Blocked by:** 03 — 業務 API 驗證原始呼叫端請求簽章。

**Status:** resolved（所有驗收項目已達成；8 項皆有 BDD Scenario 且實際執行通過；lab 暫定值已標註，未停下）

- [x] 簽章接受時間窗、時鐘容差、nonce 識別範圍、保存期與儲存方案取得確認，不能自行以未核准預設值填入。
- [x] 接受判斷驗證被簽署的時間與 nonce；缺少、過期或不符合時間容差的請求明確拒絕。
- [x] nonce 保存涵蓋簽章可接受期間及時鐘容差，期間內同一份請求不能因紀錄提前到期再次通過。
- [x] nonce 判斷與登錄抵抗併發，所有可接受同一請求的執行個體維持一致防重放結果。
- [x] 同一簽章跨至少兩個執行個體併發重送，不重複通過接受判斷。
- [x] 合法重試沿用業務識別與 Idempotency Key，改用新 nonce 重新簽署後能通過請求保護。
- [x] 防重放狀態無法可靠讀寫時，不靜默放行；明確回報錯誤且不進入業務副作用。
- [x] 整合情境驗證時間窗邊界、容差、併發重放及合法重試，清楚區分防重放與業務冪等。

對應驗收：AC-08。

## 實作紀錄（04）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；未實際執行的項目不列為已達成。

### 範圍與決策

- 授權伺服器採 OpenIddict、全專案採 API First、示範業務操作為「建立訂單」（使用者已決定）。
- 本 lab 不實作 Gateway；本單無 Gateway 驗收項。
- 防重放判斷位於 `poc/AuthSpike/Signing/SignedRequestVerifier.cs`：簽章與時間窗通過之後、業務副作用之前才登錄 nonce；重放不會進入業務處理。
- 防重放儲存：`poc/AuthSpike/Replay/NonceReplayStore.cs`（EF Core InMemory，判斷與登錄以同一把鎖序列化）。兩個建立訂單 API 執行個體共用同一個儲存物件，模擬共用狀態。
- 多個執行個體以同一公開目標（`localhost:<主要埠>`，以 Host 標頭模擬經同一入口）接收同一份簽章，與負載平衡情境一致。
- API First：`poc/AuthSpike/doc/openapi.yml` 先補充防重放說明與 503 回應，再實作。

### lab 暫定值（待使用者確認）

以下為 lab 暫定、待使用者確認的值（第 1 項以此勾選，未停下）：

- 簽章時間窗：created 不超前超過 30 秒；有效期 60 秒，即 expires 減 created 不超過 60 秒（與 03 已核准的 expires 60 秒一致）；未達 expires 才接受。
- 時鐘容差 30 秒。
- nonce 範圍為 client_id 加 nonce（同一 Client 內不可重複）。
- nonce 保存至有效期加時鐘容差（expires 加 30 秒）；已過保存期的紀錄於下次登錄時清除。
- 儲存方案：EF Core InMemory，同一程序內由多個執行個體共用。
- 防重放狀態無法可靠讀寫（lab 以 `SimulateOutage` 模擬）：回應 503 與 `{"error":"replay_state_unavailable"}`，不建立訂單。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/ReplayProtectionAndRetry.feature`

| 項目 | Scenario |
|---|---|
| 1 時間窗、時鐘容差、nonce 範圍、保存期與儲存方案（lab 暫定） | `簽章時間窗與防重放儲存參數已記錄為 lab 暫定值` |
| 2 驗證簽署時間與 nonce，缺少、過期或超出容差明確拒絕 | `時間窗邊界內的請求通過`（Outline）、`時間窗、有效期或 nonce 不符合的請求被明確拒絕`（Outline） |
| 3 nonce 保存涵蓋有效期與時鐘容差，期間內重放不通過 | `防重放紀錄保存至有效期加時鐘容差之後`、`有效期內同一份已簽章請求再次送出被拒絕` |
| 4 單一執行個體併發判斷一致 | `同一簽章併發送往單一執行個體只有一次通過` |
| 5 跨至少兩個執行個體併發重送只通過一次 | `同一簽章跨兩個執行個體併發重送只通過一次` |
| 6 合法重試沿用業務識別與 Idempotency Key，新 nonce 通過保護 | `合法重試沿用 Idempotency Key 並以新 nonce 重新簽署後通過請求保護` |
| 7 無法讀寫時不靜默放行，明確回報錯誤且不進入業務副作用 | `防重放狀態無法讀寫時回應 503 且不建立訂單` |
| 8 整合情境區分防重放與業務冪等 | `防重放與業務冪等分開的整合情境` |

**紀錄型證據（`@record`，非行為驗證）**：下列 Scenario 只檢查本紀錄的文字或勾選狀態，屬紀錄型證據，標籤為 `@record`，不冒充行為驗證：
- 簽章時間窗與防重放儲存參數已記錄為 lab 暫定值

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

測試基礎設施：`poc/AuthSpike.Tests/Support/SpikeEnvironment.cs` 新增 `[assembly: CollectionBehavior(DisableTestParallelization = true)]`，因共用的靜態執行環境在 `@isolated` 與 `@short-lifetime` Scenario 中會被替換，平行執行會互相干擾（紅燈階段曾因此出現跨 Feature 的連線失敗）。`@isolated` 標籤由 `SpikeHooks` 提供獨立執行環境。

### 未完成與明確範圍限制

- 業務去重（相同業務操作不重做副作用、Idempotency Key 保存）業務去重屬 06 單。本單的合法重試只證明通過請求保護；整合情境明確記錄「建立訂單總數為 2」，即重試仍會建立第二筆訂單。防重放不等於業務冪等。
- EF InMemory 只在同一程序內共用，不具持久性，也不足以代表多節點一致性；正式部署需改為共用資料庫並以唯一鍵約束或等效的原子操作保證。此為 lab 範圍外的實作選型，待使用者確認。
- 過期 nonce 僅於下次登錄時清除，未實作獨立排程清理。
- 保存期驗證透過 `NonceReplayStore.RetainUntilOf` 查詢（僅供測試）。

### 未決事項與阻擋

- 無阻擋。上述 lab 暫定值待使用者確認；確認後若數值不同，只需調整 `SignedRequestVerifier` 與 `NonceReplayStore` 的常數與本紀錄。

