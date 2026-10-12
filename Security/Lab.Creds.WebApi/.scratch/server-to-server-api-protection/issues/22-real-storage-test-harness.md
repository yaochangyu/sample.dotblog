# 22: 真實儲存的測試基礎與故障注入

**What to build:** 讓主要整合測試邊界（真實 mTLS 上的 HTTP）背後能啟動真實的 PostgreSQL（授權伺服器、業務 API 各一個資料庫）與 Redis，並能在測試中對這些服務製造故障（停止、重啟、清空 Redis）。這張票是後續所有儲存票的共同基礎；現有行為與測試維持不變，不改產品行為。取代原本以旗標跳過儲存的故障模擬方式作為新增能力，但舊旗標在對應票改完前仍可保留。

**Blocked by:** None (can start immediately).

**Status:** closed

- [x] 測試可以一鍵啟動兩個 PostgreSQL 資料庫與一個 Redis，並在測試結束後清理；不需要手動預先安裝。
- [x] 測試可以對任一服務製造故障：停止並重新啟動、讓連線失敗，以及清空 Redis。
- [x] 測試可以在同一測試程序內啟動同一個服務的兩個執行個體，共用同一組資料庫與 Redis。
- [x] 現有完整測試套件維持通過（不因本票新增失敗），不新增測試介面，沿用真實 Kestrel TLS 與管理端點邊界。
- [x] 至少有一個 Scenario 證明基礎設施可用：兩個執行個體共用同一個資料庫時，其中一個寫入的資料另一個看得到；停止服務後相依操作得到明確失敗。
- [x] 資料庫連線資訊走設定與環境變數，不寫進程式碼；各服務使用各自的資料庫帳號。
- [x] 不實作 Gateway 相關項目；沿用已確認的 lab 範圍決定。

### 實作紀錄
- 基礎設施：`poc/AuthSpike.Tests/Support/RealStorageTestHarness.cs` 提供兩座 PostgreSQL 容器（auth-db 與 orders-db 具各自獨立隨機密碼帳號，可由環境變數覆寫）與一座 Redis 容器。
- 測試生命週期與故障注入：提供 `StopAsync`、`StartAsync`、`FlushRedisAsync`；在 `AfterScenario` 聚合重啟例外明確揭露錯誤。
- 驗證情境：`poc/AuthSpike.Tests/Features/RealStorageTestHarness.feature` 包含雙執行個體跨 instance 可見性、DB 隔離性、停止與重啟故障恢復、Redis 清空後資料消失等 Scenarios，測試全綠通過。
- 審查流程：經 Copilot CLI (`gpt-5.5`) 進行 Standards/Spec 雙軸審查，針對 AfterScenario 例外隱匿完成重構與驗證。
