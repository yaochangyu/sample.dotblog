@real-storage
Feature: 業務 API 資料庫與跨執行個體的業務冪等
  依 23 單驗收項目，訂單、冪等紀錄、處理中租約與業務唯一鍵儲存至 PostgreSQL 資料庫。
  兩個業務 API 執行個體共用資料庫時，同一筆業務操作併發送達只提交一次，租約到期可由另一執行個體接手。
  業務 API 重啟後狀態仍在，資料庫不可用時回報明確服務錯誤。

  Background:
    Given 真實儲存測試環境已就緒且業務 API 連線至 "orders-db"

  Scenario: 兩個執行個體併發提交同一業務操作只提交一次
    When 簽章呼叫端以 10 組新簽章與新 Idempotency-Key 併發送出訂單 "ref-multi-instance"，品項 "book"，數量 1，交替送往真實資料庫的兩個執行個體
    Then 併發回應只包含 201 或 409
    And 201 回應的次數剛好為 1
    And 訂單 "ref-multi-instance" 於資料庫中的業務訂單數為 1

  Scenario: 業務 API 重啟後已建立的訂單與去重狀態仍然存在
    Given 簽章呼叫端送出訂單 "ref-restart"，品項 "gadget"，數量 2
    When 業務 API 重新啟動
    Then 查詢訂單 "ref-restart" 仍然存在且狀態為有效
    When 簽章呼叫端以相同 Idempotency-Key 重送訂單 "ref-restart"，品項 "gadget"，數量 2
    Then 最近一次真實訂單請求回應 201
    And 最近一次真實訂單請求標示為重播
    And 訂單 "ref-restart" 於資料庫中的業務訂單數為 1

  Scenario: 資料庫不可用時建單請求回報服務錯誤且無業務副作用
    When 停止 "orders-db"
    And 簽章呼叫端送出訂單 "ref-db-down"，品項 "tool"，數量 5
    Then 最近一次真實訂單請求回應 503
    When 重新啟動 "orders-db"
    Then 訂單 "ref-db-down" 於資料庫中的業務訂單數為 0

  Scenario: 相同 Idempotency-Key 但業務識別不同時被拒絕且不覆寫
    Given 簽章呼叫端以 Idempotency-Key "pg-conflict-key" 送出訂單 "ref-conflict-1"，品項 "book"，數量 1
    Then 最近一次真實訂單請求回應 201
    When 簽章呼叫端以 Idempotency-Key "pg-conflict-key" 送出訂單 "ref-conflict-2"，品項 "pen"，數量 2
    Then 最近一次真實訂單請求回應 422
    And 訂單 "ref-conflict-2" 於資料庫中的業務訂單數為 0

  Scenario: 租約逾期後由另一執行個體接手業務提交
    Given 業務 API 提交於寫入前暫停
    When 簽章呼叫端在背景送出訂單 "ref-lease"，品項 "tablet"，數量 1 至主要執行個體
    And 等待業務提交進入處理中暫停
    And 業務操作在資料庫中的租約已手動逾期
    When 簽章呼叫端以新嘗試送出訂單 "ref-lease"，品項 "tablet"，數量 1 至第二個執行個體
    Then 最近一次真實訂單請求回應 201
    And 訂單 "ref-lease" 於資料庫中的業務訂單數為 1
