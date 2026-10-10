Feature: 業務 API 驗證原始呼叫端的 HTTP 請求簽章
  依 03 單驗收項目，確認建立訂單與查詢訂單 API 只接受同一 Client 金鑰簽署、內容與目標未被竄改的請求。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API；本 lab 不宣稱 nonce 防重放已完成。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 已登錄服務 "billing-client" 與其用戶端憑證
    And 簽章呼叫端 "orders-client" 已以 mTLS 取得 Token

  Scenario: 共同簽章規則已由使用者核准並記錄
    Then 03 單第 1 項驗收已勾選
    And 03 單狀態為 "resolved"
    And 03 單的實作紀錄包含 "ecdsa-p256-sha256"
    And 03 單的實作紀錄包含 "使用者已核准"
    And 03 單的實作紀錄包含 "created 不超前 30 秒"
    And 03 單的實作紀錄包含 "expires 60 秒"

  Scenario: 已簽章的建立訂單請求通過
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    Then 業務 API 回應 201
    And 03 單第 2 項驗收已勾選

  Scenario: 已簽章的查詢訂單請求通過（無 Body）
    Given 簽章呼叫端 "orders-client" 已建立一筆訂單
    When 簽章呼叫端以 "orders-client" 送出已簽章的查詢訂單請求
    Then 業務 API 回應 200
    And 03 單第 2 項驗收已勾選

  Scenario: 未簽章的建立訂單請求被拒絕
    When 持有 Token 的呼叫端未簽章送出建立訂單請求
    Then 業務 API 回應 401
    And 03 單第 2 項驗收已勾選

  Scenario: 未簽章的查詢訂單請求被拒絕
    Given 簽章呼叫端 "orders-client" 已建立一筆訂單
    When 持有 Token 的呼叫端未簽章送出查詢訂單請求
    Then 業務 API 回應 401
    And 03 單第 2 項驗收已勾選

  Scenario: 無 Body 的查詢簽章改動目標被拒絕
    Given 簽章呼叫端 "orders-client" 已建立一筆訂單
    When 簽章呼叫端以 "orders-client" 送出已簽章的查詢訂單請求，但目標改為其他訂單編號
    Then 業務 API 回應 401
    And 03 單第 2 項驗收已勾選

  Scenario: 已簽章的查詢參數被改動被拒絕
    Given 簽章呼叫端 "orders-client" 已建立一筆訂單
    When 簽章呼叫端以 "orders-client" 送出已簽章的查詢訂單請求，查詢參數為 "view=summary"，但送出時改為 "view=full"
    Then 業務 API 回應 401
    And 03 單第 4 項驗收已勾選

  Scenario: 有 Body 時簽章涵蓋內容摘要與內容型別
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    Then 已簽章請求的 Signature-Input 涵蓋 "content-digest" 與 "content-type"
    And 03 單第 3 項驗收已勾選

  Scenario Outline: 缺少必要簽章標頭被拒絕
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，但移除 "<標頭>" 標頭
    Then 業務 API 回應 401
    And 03 單第 6 項驗收已勾選

    Examples:
      | 標頭             |
      | Signature        |
      | Signature-Input  |
      | Content-Digest   |
      | Idempotency-Key  |

  Scenario Outline: 改動已簽署的 <項目> 被拒絕
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，並改動 "<項目>"
    Then 業務 API 回應 401
    And 03 單第 6 項驗收已勾選

    Examples:
      | 項目                 |
      | 目標                 |
      | Idempotency-Key 標頭 |
      | Content-Type 標頭    |
      | Body                 |
      | 授權 Token           |

  Scenario: 已逾期的簽章被拒絕
    When 簽章呼叫端以 "orders-client" 送出簽章已逾期的建立訂單請求
    Then 業務 API 回應 401
    And 03 單第 6 項驗收已勾選

  Scenario: 簽章金鑰與 mTLS 用戶端憑證私鑰分開
    Then 簽章金鑰的公開部分與 "orders-client" 用戶端憑證公開金鑰不同
    And 03 單第 5 項驗收已勾選

  Scenario: 持有 orders-client Token 但以 billing-client 簽章金鑰簽署被拒絕
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，但以 "billing-client" 的簽章金鑰簽署
    Then 業務 API 回應 401
    And 03 單第 5 項驗收已勾選
    And 03 單第 6 項驗收已勾選

  Scenario: 實際簽署與驗證的整合情境區分合法與竄改請求
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求
    Then 業務 API 回應 201
    When 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，並改動 "Body"
    Then 業務 API 回應 401
    And 03 單的實作紀錄包含 "未完成防重放"
    And 03 單第 8 項驗收已勾選

  Scenario: Gateway 項目標註為本 lab 不實作且略過
    Then 03 單第 7 項驗收已勾選
    And 03 單的實作紀錄包含 "本 lab 不實作，略過"
