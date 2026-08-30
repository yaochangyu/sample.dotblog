Feature: 接收大型 JSON 請求處理
  為了避免高並發下每個請求都在 LOH 產生大型垃圾陣列
  作為一個高效能 Web API
  系統應該支援 ArrayPool 池化反序列化與 IAsyncEnumerable 串流解析

  Scenario: 使用 ArrayPool 接收 524,288 筆 double 數值並計算平均值
    Given 準備了 524288 筆 double 數值陣列
    When 發送 POST 請求至 "/api/readings"
    Then API 回傳狀態碼 200
    And 回傳的總筆數應該為 524288
    And 回傳的總和應該大於 0

  Scenario: 使用 IAsyncEnumerable 串流接收 20,000 筆會員結構資料
    Given 準備了 20000 筆會員帳號資料
    When 以串流方式發送 POST 請求至 "/api/members-stream"
    Then API 回傳狀態碼 200
    And 回傳的會員總數應該為 20000
    And 啟用中的會員數應該大於 0

  Scenario: 使用 ArrayPool 接收 50,000 筆字串陣列
    Given 準備了 50000 筆字串陣列
    When 發送 POST 請求至 "/api/strings"
    Then API 回傳狀態碼 200
    And 回傳的字串總筆數應該為 50000
    And 回傳的總長度應該大於 0
