Feature: 手動按日索引日誌管理 (Daily Index)
  作為系統維運與開發人員
  我想透過手動按日索引模式寫入日誌並支援跨日範圍檢索
  以驗證 Time-based Daily Index 模式之相容性

  Background:
    Given Elasticsearch 服務已正常運作
    And 系統 API 服務已啟動

  Scenario: 透過 API 寫入單日日誌
    Given 我有一筆單日日誌資料:
      | Service          | Level   | Message                   | TraceId           |
      | inventory-service | Warning | 庫存不足警告: 商品 SKU-8888 | trace-daily-bdd-01 |
    When 我發送 POST 請求至 "/api/daily-index/logs" 寫入該單日日誌
    Then API 應回傳 HTTP 狀態碼 201
    And 回傳內容應包含所建立的日誌資訊

  Scenario: 透過 API 執行跨單日索引範圍查詢日誌
    Given 單日索引中已寫入以下日誌:
      | DaysAgo | Service          | Level | Message                  | TraceId           |
      | 0       | shipping-service | Info  | 包裹出貨完成 TRK-2001     | trace-daily-bdd-02 |
      | 1       | shipping-service | Info  | 包裹已抵達轉運中心 TRK-2001 | trace-daily-bdd-03 |
    When 我發送 GET 請求至 "/api/daily-index/logs" 查詢服務 "shipping-service" 且關鍵字為 "TRK-2001" 涵蓋過去 1 天範圍
    Then API 應回傳 HTTP 狀態碼 200
    And 查詢結果應包含至少 2 筆日誌
