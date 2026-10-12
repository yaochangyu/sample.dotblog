@real-storage
Feature: 真實儲存的測試基礎與故障注入
  測試可一鍵啟動兩座 PostgreSQL（auth-db、orders-db，各自帳號）與一座 Redis，
  並能停止、重啟服務與清空 Redis，供後續儲存相關票共用。

  Scenario: 兩個執行個體共用同一個資料庫時互相看得到資料
    Given 同一個服務的兩個執行個體連到 "auth-db"
    When 執行個體 A 寫入資料 "client-001"
    Then 執行個體 B 讀得到資料 "client-001"

  Scenario: 兩座資料庫彼此獨立
    Given 同一個服務的兩個執行個體連到 "auth-db"
    When 執行個體 A 寫入資料 "client-002"
    Then "orders-db" 讀不到資料 "client-002"

  Scenario Outline: 停止 <服務> 後相依操作得到明確失敗，重啟後恢復
    Given 同一個服務的兩個執行個體連到 "<服務>"
    When 停止 "<服務>"
    Then 執行個體 A 的操作得到明確失敗
    When 重新啟動 "<服務>"
    Then 執行個體 B 的操作成功

    Examples:
      | 服務      |
      | auth-db   |
      | orders-db |
      | redis     |

  Scenario: 清空 Redis 後先前的資料消失
    Given 同一個服務的兩個執行個體連到 "redis"
    When 執行個體 A 寫入資料 "nonce-001"
    And 清空 Redis
    Then 執行個體 B 讀不到資料 "nonce-001"
