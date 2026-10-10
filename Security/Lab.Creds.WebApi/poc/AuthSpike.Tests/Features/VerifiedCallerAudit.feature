Feature: 安全追查已驗證呼叫者
  依 09 單驗收項目，確認每筆業務呼叫都能追查到已驗證 Client、金鑰、操作與結果；
  失敗或偽造身分的請求不會被記為已驗證呼叫者；紀錄不含原始 Token、私鑰、完整敏感 Body 與簽章基底；
  稽核紀錄寫入失敗時明確回報錯誤，不靜默吞掉、也不假報成功。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 已登錄服務 "billing-client" 與其用戶端憑證
    And 稽核呼叫端 "orders-client" 已以 mTLS 取得 Token

  @audit
  Scenario: 成功驗證的建立訂單請求留下含已驗證 Client、金鑰識別、操作、結果與關聯識別的紀錄
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    Then 稽核呼叫端最近一次請求回應 201
    And 最近一次請求的稽核紀錄結果為 "accepted"，原因為 "signature_verified"
    And 最近一次請求的稽核紀錄已驗證 Client 為 "orders-client"
    And 最近一次請求的稽核紀錄簽章金鑰識別為 "orders-client-lab-sig-1"
    And 最近一次請求的稽核紀錄操作為 "POST /orders"
    And 最近一次請求的稽核紀錄憑證指紋與 "orders-client" 用戶端憑證一致
    And 最近一次請求的稽核紀錄的關聯識別與回應 X-Correlation-Id 相同
    And 最近一次請求的稽核紀錄最終業務結果為 201

  @audit
  Scenario: 未簽章的請求不被記為已驗證，宣稱的 Client 標示為未驗證
    When 稽核呼叫端以 "orders-client" 未簽章送出建立訂單請求，並附帶 X-Client-Id 標頭 "billing-client"
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "signature_rejected"
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 Token Client 為 "orders-client"
    And 最近一次請求的稽核紀錄未驗證的 X-Client-Id 宣稱為 "billing-client"

  @audit
  Scenario: 以他人簽章金鑰簽署的請求不與已驗證呼叫者混淆
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，但以 "billing-client" 的簽章金鑰簽署
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "signature_rejected"
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 Token Client 為 "orders-client"

  @audit
  Scenario: 無效 Token 的請求宣稱的 Client 不被記為已驗證
    When 稽核呼叫端持無效 Token 送出建立訂單請求，並附帶 X-Client-Id 標頭 "orders-client"
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "caller_rejected"
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 X-Client-Id 宣稱為 "orders-client"

  @audit
  Scenario: 重送同一份已簽章請求的紀錄為 rejected 且不記為已驗證
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    And 稽核呼叫端重送同一份已簽章的建立訂單請求
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "signature_replayed"
    And 最近一次請求的稽核紀錄最終業務結果為 401
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 Token Client 為 "orders-client"

  @audit
  Scenario: 稽核紀錄與成功回應不含原始 Token、簽章私鑰與完整敏感 Body
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，品項為 "機密品項-稽核測試"
    Then 稽核呼叫端最近一次請求回應 201
    And 稽核紀錄全部不含原始 Token
    And 稽核紀錄全部不含 "機密品項-稽核測試"
    And 稽核紀錄全部不含 "orders-client" 的簽章私鑰

  @audit
  Scenario: 錯誤回應與簽章驗證紀錄不含 Token、Signature 標頭或簽章基底
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，並改動 Body
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次回應不含原始 Token、Signature 或 Signature-Input 標頭值與簽章基底
    And 稽核紀錄全部不含原始 Token
    And 稽核紀錄全部不含簽章基底與 Signature 標頭值

  @audit
  Scenario: 無授權範圍的 Token 被拒絕，錯誤回應與紀錄不洩漏 Token
    When 稽核呼叫端以 "orders-api" 持無 orders 授權範圍的 Token 送出建立訂單請求
    Then 稽核呼叫端最近一次請求回應 403
    And 最近一次請求的稽核紀錄結果為 "denied"，原因為 "status_403"
    And 最近一次請求的稽核紀錄最終業務結果為 403
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 Token Client 為 "orders-api"
    And 最近一次回應不含原始 Token、Signature 或 Signature-Input 標頭值與簽章基底

  @audit
  Scenario: 業務處理的最終結果（422 冪等鍵衝突）也記入同一筆稽核紀錄
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，業務識別為 "audit-ref-422"，Idempotency-Key 為 "audit-key-422-a"
    And 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，業務識別為 "audit-ref-422-other"，Idempotency-Key 為 "audit-key-422-a"
    Then 稽核呼叫端最近一次請求回應 422
    And 最近一次請求的稽核紀錄結果為 "accepted"，原因為 "signature_verified"
    And 最近一次請求的稽核紀錄最終業務結果為 422
    And 稽核紀錄全部不含 "audit-ref-422-other"

  @audit
  Scenario: 稽核紀錄與防重放儲存分開管理，保存期與存取規則已記錄為 lab 暫定
    Then 稽核紀錄儲存與防重放儲存為不同物件
    And 稽核紀錄保存期為 90 天
    And 稽核紀錄不經業務 API 公開，查詢 "/audit" 回應 404
    Given 09 單的實作紀錄可讀取
    Then 09 單的實作紀錄包含 "保存期"
    And 09 單的實作紀錄包含 "存取規則"
    And 09 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 09 單第 5 項驗收已勾選

  @audit
  Scenario: 成功、失敗與偽造身分的情境可依關聯識別正確對應且不含禁止資料
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    And 稽核呼叫端把最近一次請求的關聯識別記為 "成功"
    And 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，但以 "billing-client" 的簽章金鑰簽署
    And 稽核呼叫端把最近一次請求的關聯識別記為 "偽造"
    And 稽核呼叫端持無效 Token 送出建立訂單請求，並附帶 X-Client-Id 標頭 "orders-client"
    And 稽核呼叫端把最近一次請求的關聯識別記為 "失敗"
    Then 依關聯識別 "成功" 的稽核紀錄結果為 "accepted"，已驗證 Client 為 "orders-client"
    And 依關聯識別 "偽造" 的稽核紀錄結果為 "rejected"，已驗證 Client 為空
    And 依關聯識別 "失敗" 的稽核紀錄結果為 "rejected"，已驗證 Client 為空
    And 稽核紀錄全部不含原始 Token

  @audit @isolated
  Scenario: 簽章通過但稽核紀錄寫入失敗時拒絕建立訂單，回報 audit_unavailable
    Given 稽核紀錄暫時無法寫入
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    Then 稽核呼叫端最近一次請求回應 503
    And 稽核呼叫端最近一次請求的錯誤代碼為 "audit_unavailable"
    And 稽核情境中業務 API 建立訂單總數為 0

  @audit @isolated
  Scenario: 呼叫者查證失敗的拒絕紀錄寫入失敗時回報 audit_unavailable 而非 401
    Given 稽核紀錄暫時無法寫入
    When 稽核呼叫端持無效 Token 送出建立訂單請求，並附帶 X-Client-Id 標頭 "orders-client"
    Then 稽核呼叫端最近一次請求回應 503
    And 稽核呼叫端最近一次請求的錯誤代碼為 "audit_unavailable"
