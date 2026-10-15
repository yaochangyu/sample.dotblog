Feature: 統一的已驗證呼叫者與呼叫者類型（前置重構）
  依 01 單驗收項目，確認服務呼叫路徑改為產出統一的已驗證呼叫者後，服務保護行為不變；
  驗證失敗時，請求宣稱的呼叫者類型與身分只標為未驗證，不產出已驗證呼叫者。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 已登錄服務 "billing-client" 與其用戶端憑證
    And 稽核呼叫端 "orders-client" 已以 mTLS 取得 Token

  @unified-caller @isolated
  Scenario: AC-D02 服務呼叫經 Token 與簽章驗證後以已驗證呼叫者提交業務，只提交一次
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    Then 稽核呼叫端最近一次請求回應 201
    And 最近一次請求的稽核紀錄結果為 "accepted"，原因為 "signature_verified"
    And 最近一次請求的稽核紀錄已驗證 Client 為 "orders-client"
    And 稽核情境中業務 API 建立訂單總數為 1

  @unified-caller @isolated
  Scenario: AC-D02 服務呼叫未簽章時被拒絕且不產生業務副作用
    When 稽核呼叫端以 "orders-client" 未簽章送出建立訂單請求，並附帶 X-Client-Id 標頭 "billing-client"
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "signature_rejected"
    And 稽核情境中業務 API 建立訂單總數為 0

  @unified-caller @isolated
  Scenario: AC-D02 服務呼叫重送已簽章請求被拒絕，業務副作用仍只有一次
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    And 稽核呼叫端重送同一份已簽章的建立訂單請求
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "signature_replayed"
    And 稽核情境中業務 API 建立訂單總數為 1

  @unified-caller @isolated
  Scenario: AC-D02 服務呼叫以他人簽章金鑰簽署時被拒絕且不產生業務副作用
    When 稽核呼叫端以 "orders-client" 送出已簽章的建立訂單請求，但以 "billing-client" 的簽章金鑰簽署
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄結果為 "rejected"，原因為 "signature_rejected"
    And 稽核情境中業務 API 建立訂單總數為 0

  @unified-caller @isolated
  Scenario: AC-D02 Token 有效但缺少 orders 授權範圍的服務呼叫回應 403 且不產生業務副作用
    When 稽核呼叫端以 "orders-api" 持無 orders 授權範圍的 Token 送出建立訂單請求
    Then 稽核呼叫端最近一次請求回應 403
    And 稽核情境中業務 API 建立訂單總數為 0

  @unified-caller @isolated
  Scenario: AC-D02 無效 Token 的服務呼叫被拒絕，宣稱的 Client 只標為未驗證
    When 稽核呼叫端持無效 Token 送出建立訂單請求，並附帶 X-Client-Id 標頭 "orders-client"
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 X-Client-Id 宣稱為 "orders-client"
    And 稽核情境中業務 API 建立訂單總數為 0

  @unified-caller @isolated
  Scenario: 驗證失敗的服務呼叫宣稱的呼叫者身分只標為未驗證，不產出已驗證呼叫者
    When 稽核呼叫端以 "orders-client" 未簽章送出建立訂單請求，並附帶 X-Client-Id 標頭 "billing-client"
    Then 稽核呼叫端最近一次請求回應 401
    And 最近一次請求的稽核紀錄沒有已驗證 Client
    And 最近一次請求的稽核紀錄未驗證的 Token Client 為 "orders-client"
    And 最近一次請求的稽核紀錄未驗證的 X-Client-Id 宣稱為 "billing-client"

  @record
  Scenario: 01 單驗收項目已逐項勾選且狀態為 resolved
    Then 01 單第 1 項驗收已勾選
    And 01 單第 2 項驗收已勾選
    And 01 單第 3 項驗收已勾選
    And 01 單第 4 項驗收已勾選
    And 01 單第 5 項驗收已勾選
    And 01 單第 6 項驗收已勾選
    And 01 單的狀態為 resolved
