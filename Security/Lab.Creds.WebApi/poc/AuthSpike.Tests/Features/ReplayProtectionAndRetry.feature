Feature: 跨執行個體防重放並支援合法重簽重試
  依 04 單驗收項目，確認同一份簽章即使併發送往不同建立訂單 API 執行個體，也只會被接受一次；
  合法重試沿用業務識別與 Idempotency Key，改用新 nonce 重新簽署後可通過請求保護。
  防重放與業務冪等是不同保證：本 feature 不宣稱業務去重已完成（屬 06 單）。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 已登錄服務 "billing-client" 與其用戶端憑證
    And 簽章呼叫端 "orders-client" 已以 mTLS 取得 Token

  Scenario: 簽章時間窗與防重放儲存參數已記錄為 lab 暫定值
    Then 04 單第 1 項驗收已勾選
    And 04 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 04 單的實作紀錄包含 "時鐘容差 30 秒"
    And 04 單的實作紀錄包含 "有效期 60 秒"
    And 04 單的實作紀錄包含 "nonce 範圍為 client_id 加 nonce"
    And 04 單的實作紀錄包含 "保存至有效期加時鐘容差"
    And 04 單的實作紀錄包含 "EF InMemory"

  Scenario Outline: 時間窗邊界內的請求通過
    When 簽章呼叫端以 "orders-client" 送出時間參數為 "<條件>" 的建立訂單請求
    Then 最近一次請求回應 201
    And 04 單第 2 項驗收已勾選

    Examples:
      | 條件              |
      | created 超前 25 秒 |
      | 有效期 60 秒       |

  Scenario Outline: 時間窗、有效期或 nonce 不符合的請求被明確拒絕
    When 簽章呼叫端以 "orders-client" 送出時間參數為 "<條件>" 的建立訂單請求
    Then 最近一次請求回應 401
    And 04 單第 2 項驗收已勾選

    Examples:
      | 條件              |
      | created 超前 35 秒 |
      | 有效期 61 秒       |
      | 已過期            |
      | nonce 為空        |
      | 缺少 created 參數 |

  Scenario: 防重放紀錄保存至有效期加時鐘容差之後
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "retention-key"
    Then 最近一次請求回應 201
    And 防重放紀錄保存至該請求有效期加 30 秒
    And 04 單第 3 項驗收已勾選

  Scenario: 有效期內同一份已簽章請求再次送出被拒絕
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "resend-key"
    Then 第一次送出的回應為 201
    When 同一份已簽章請求再次送出到主要執行個體
    Then 最近一次請求回應 401
    And 04 單第 3 項驗收已勾選

  @isolated
  Scenario: 同一簽章併發送往單一執行個體只有一次通過
    When 簽章呼叫端以 "orders-client" 併發送出同一份已簽章的建立訂單請求 10 次到主要執行個體
    Then 通過的請求數為 1
    And 被拒絕的請求回應 401
    And 業務 API 建立訂單數為 1
    And 04 單第 4 項驗收已勾選

  @isolated
  Scenario: 同一簽章跨兩個執行個體併發重送只通過一次
    Given 第二個建立訂單 API 執行個體已啟動並共用防重放儲存
    When 簽章呼叫端以 "orders-client" 併發送出同一份已簽章的建立訂單請求 10 次，交替送往兩個執行個體
    Then 通過的請求數為 1
    And 被拒絕的請求回應 401
    And 兩個執行個體建立訂單總數為 1
    And 04 單第 5 項驗收已勾選

  Scenario: 合法重試沿用 Idempotency Key 並以新 nonce 重新簽署後通過請求保護
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "retry-key-1"
    Then 最近一次請求回應 201
    When 簽章呼叫端 "orders-client" 以相同 Idempotency-Key "retry-key-1" 與新 nonce 重新簽署並重試
    Then 最近一次請求回應 201
    And 重試使用的 nonce 與原請求不同
    And 04 單第 6 項驗收已勾選

  @isolated
  Scenario: 防重放狀態無法讀寫時回應 503 且不建立訂單
    Given 防重放儲存暫時無法讀寫
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "outage-key"
    Then 最近一次請求回應 503
    And 最近一次請求的錯誤代碼為 "replay_state_unavailable"
    And 業務 API 建立訂單數為 0
    And 04 單第 7 項驗收已勾選

  @isolated
  Scenario: 防重放與業務冪等分開的整合情境
    Given 第二個建立訂單 API 執行個體已啟動並共用防重放儲存
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "integration-key"
    Then 最近一次請求回應 201
    When 同一份已簽章請求於第二個執行個體重送
    Then 最近一次請求回應 401
    When 簽章呼叫端 "orders-client" 以相同 Idempotency-Key "integration-key" 與新 nonce 重新簽署並重試
    Then 最近一次請求回應 201
    And 兩個執行個體建立訂單總數為 2
    And 04 單的實作紀錄包含 "業務去重屬 06 單"
    And 04 單第 8 項驗收已勾選
