Feature: 管理者撤銷與查證故障時拒絕處理
  依 07 單驗收項目，確認撤銷 Client、Token、mTLS 憑證或簽章金鑰後，各驗證端於 60 秒內阻擋後續請求；
  查證服務故障且沒有時效內有效快取時 fail closed，明確回報 503 且不進入業務副作用。
  撤銷不回滾已提交的業務操作。本 lab 不實作 Gateway，呼叫端直接呼叫業務 API。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 07 單的實作紀錄可讀取

  @record
  Scenario: 撤銷操作、狀態同步、快取與故障回應契約已記錄
    Then 07 單第 1 項驗收已勾選
    And 07 單的實作紀錄包含 "撤銷契約"
    And 07 單的實作紀錄包含 "查證快取"
    And 07 單的實作紀錄包含 "不新增管理 UI"
    And 07 單的 API 契約包含 "verification_unavailable"

  @isolated
  Scenario Outline: 撤銷<對象>後後續請求在 60 秒內被阻擋
    Given 簽章呼叫端 "orders-client" 以 mTLS 取得 Token 並成功建立訂單 "撤銷前訂單"
    And 簽章呼叫端 "orders-client" 的查詢請求回應 200
    When 管理者撤銷 "<對象>"
    Then 撤銷後的查詢請求於 60 秒內回應 401
    And 07 單第 2 項驗收已勾選

    Examples:
      | 對象       |
      | Token      |
      | Client     |
      | mTLS 憑證  |
      | 簽章金鑰   |

  @isolated
  Scenario Outline: 撤銷<對象>後既有 Token 仍為有效，但接受判斷仍拒絕
    Given 簽章呼叫端 "orders-client" 以 mTLS 取得 Token 並成功建立訂單 "撤銷前訂單"
    When 管理者撤銷 "<對象>"
    Then 授權伺服器對既有 Token 的 introspection 仍為 active
    And 撤銷後的查詢請求於 60 秒內回應 401
    And 07 單第 5 項驗收已勾選

    Examples:
      | 對象       |
      | Client     |
      | mTLS 憑證  |
      | 簽章金鑰   |

  @isolated
  Scenario: 撤銷 Token 後兩個執行個體於 60 秒內拒絕後續請求
    Given 第二個建立訂單 API 執行個體已啟動並共用防重放儲存
    And 簽章呼叫端 "orders-client" 以 mTLS 取得 Token 並成功建立訂單 "多執行個體訂單"
    And 簽章呼叫端 "orders-client" 的查詢請求於第二個執行個體回應 200
    When 管理者撤銷 "Token"
    Then 撤銷後的查詢請求於 60 秒內回應 401
    And 第二個執行個體的撤銷後查詢於 60 秒內回應 401
    And 07 單第 3 項驗收已勾選

  @isolated
  Scenario: 既有連線與第二個執行個體上的後續請求同樣被阻擋
    Given 第二個建立訂單 API 執行個體已啟動並共用防重放儲存
    And 簽章呼叫端 "orders-client" 以既有連線取得 Token 並成功建立訂單 "連線訂單"
    And 簽章呼叫端 "orders-client" 的查詢請求於第二個執行個體回應 200
    When 管理者撤銷 "Token"
    Then 撤銷後的查詢請求於 60 秒內回應 401
    And 後續查詢請求使用的連線與撤銷前相同
    And 第二個執行個體的撤銷後查詢於 60 秒內回應 401
    And 07 單第 4 項驗收已勾選

  @isolated
  Scenario: 60 秒門檻的測量起點與拒絕結果已記錄於實作紀錄
    Given 簽章呼叫端 "orders-client" 以 mTLS 取得 Token 並成功建立訂單 "測量訂單"
    When 管理者撤銷 "Token"
    Then 撤銷後的查詢請求於 60 秒內回應 401
    And 實際測得的撤銷延遲小於 60 秒
    And 07 單的實作紀錄包含 "測量起點"
    And 07 單的實作紀錄包含 "拒絕時間"
    And 07 單的實作紀錄包含 "60 秒門檻"
    And 07 單第 9 項驗收已勾選

  @short-cache
  Scenario: 查證服務故障時快取有效期間內可接受，且失敗不延長快取有效期限
    Given 簽章呼叫端 "orders-client" 以 mTLS 取得 Token 並成功建立訂單 "快取前訂單"
    And 授權伺服器停止服務
    When 在快取有效期限內送出查詢請求
    Then 被快取允許的查詢回應為 200
    When 等待查證快取過期後連續送出 2 次查詢請求
    Then 所有查詢回應為 503 且錯誤代碼為 "verification_unavailable"
    And 查詢回應不是憑證無效的 401
    And 07 單第 6 項驗收已勾選
    And 07 單第 7 項驗收已勾選

  @isolated
  Scenario: 查證服務故障且沒有有效快取時拒絕處理且不建立訂單
    Given 簽章呼叫端 "orders-client" 以 mTLS 取得 Token
    And 授權伺服器停止服務
    When 授權伺服器停止後，簽章呼叫端 "orders-client" 以 Idempotency-Key "outage-key" 送出建立訂單請求
    Then 最後一次嘗試的回應為 503 且錯誤代碼為 "verification_unavailable"
    And 最後一次嘗試的回應不是憑證無效的 401
    And 業務 API 建立訂單數為 0
    And 07 單第 6 項驗收已勾選
    And 07 單第 7 項驗收已勾選

  @isolated
  Scenario: 撤銷後被拒絕的寫入不建立訂單，撤銷不回滾已提交的訂單
    Given 簽章呼叫端 "orders-client" 以 mTLS 取得 Token 並成功建立訂單 "已提交訂單"
    When 管理者撤銷 "Token"
    Then 撤銷後的查詢請求於 60 秒內回應 401
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "after-revoke" 送出建立訂單請求
    Then 最後一次嘗試的回應為 401
    And 業務 API 建立訂單數為 1
    And 重新以 mTLS 取得新 Token 後查詢 "已提交訂單" 的狀態仍為 "active"
    And 07 單第 8 項驗收已勾選
