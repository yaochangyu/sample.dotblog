Feature: 回傳大型 JSON 串流響應處理
  為了避免伺服器端與用戶端因大回傳資料產生 LOH 飆升
  作為一個高效能 Web API 用戶端與伺服端
  系統應該支援 IAsyncEnumerable 邊產邊傳與 Client 0 LOH 逐筆接收

  Scenario: 伺服器端串流回傳 524,288 筆 double 數值且 Client 端逐筆接收
    When 用戶端以串流方式發送 GET 請求至 "/api/export-readings-stream"
    Then 用戶端應該成功接收 524288 筆數值
    And 接收數值的累加總和應該大於 0

  Scenario: 伺服器端串流回傳 20,000 筆會員帳號資料且 Client 端逐筆解析
    When 用戶端以串流方式發送 GET 請求至 "/api/export-members-stream"
    Then 用戶端應該成功接收 20000 筆會員資料
    And 接收到的啟用會員數應該大於 0

  Scenario: 伺服器端串流回傳 50,000 筆字串資料且 Client 端逐筆解析
    When 用戶端以串流方式發送 GET 請求至 "/api/export-strings-stream"
    Then 用戶端應該成功接收 50000 筆字串
    And 接收到的字串總長度應該大於 0
