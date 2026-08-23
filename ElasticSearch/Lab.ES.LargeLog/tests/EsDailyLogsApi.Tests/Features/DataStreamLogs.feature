Feature: 現代 Data Stream 日誌管理
  作為系統維運與開發人員
  我想透過 API 將日誌寫入 Elasticsearch Data Stream 並進行查詢、更新與刪除
  以便高效管理海量日誌

  Background:
    Given Elasticsearch 服務已正常運作
    And 系統 API 服務已啟動

  Scenario: 透過 API 寫入日誌並經由背景批次處理器寫入 Data Stream
    Given 我有一筆日誌資料:
      | Service       | Level       | Message                    | TraceId       |
      | order-service | Information | 訂單建立成功，訂單編號 ORD-1001 | trace-bdd-001 |
    When 我發送 POST 請求至 "/api/logs" 寫入該日誌
    Then API 應回傳 HTTP 狀態碼 202
    And 等待背景批次處理器將日誌寫入 Data Stream
    Then 透過 Data Stream 全文檢索關鍵字 "ORD-1001" 應能查得該筆日誌

  Scenario: 透過 API 依 ID 查詢單筆日誌
    Given Data Stream 中已存在一筆日誌:
      | Service         | Level       | Message                   | TraceId       |
      | payment-service | Information | 信用卡授權扣款完成 NT$ 1500 | trace-bdd-002 |
    When 我發送 GET 請求依日誌 ID 查詢該筆日誌
    Then API 應回傳 HTTP 狀態碼 200
    And 回傳的日誌內容訊息應為 "信用卡授權扣款完成 NT$ 1500"

  Scenario: 透過 API 依底層索引與 ID 更新日誌訊息
    Given Data Stream 中已存在一筆日誌:
      | Service      | Level   | Message             | TraceId       |
      | auth-service | Warning | 使用者密碼嘗試失敗 1 次 | trace-bdd-003 |
    When 我發送 PUT 請求至該日誌所屬底層索引更新訊息為 "使用者密碼嘗試失敗 3 次（帳號鎖定）"
    Then API 應回傳 HTTP 狀態碼 204
    And 依 ID 重新取得日誌其訊息應更新為 "使用者密碼嘗試失敗 3 次（帳號鎖定）"

  Scenario: 透過 API 依底層索引與 ID 刪除日誌
    Given Data Stream 中已存在一筆日誌:
      | Service       | Level | Message             | TraceId       |
      | audit-service | Debug | 暫存稽核日誌待刪除    | trace-bdd-004 |
    When 我發送 DELETE 請求至該日誌所屬底層索引刪除該筆日誌
    Then API 應回傳 HTTP 狀態碼 204
    And 依 ID 重新取得該日誌應回傳 HTTP 狀態碼 404
