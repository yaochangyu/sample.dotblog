Feature: 請求重試機制
    作為一個 API 使用者
    我希望能夠使用 Request ID 重試處理
    以便獲得之前提交的請求處理結果

    Background: 系統初始狀態
        Given 系統速率限制為每分鐘 2 個請求
        And 請求池中有一些等待的請求

    Scenario: 使用有效 Request ID 重試成功
        Given 請求池中存在 Request ID "valid-request-id"
        And 該請求狀態為 "Pending"
        And 系統可以處理新請求
        When 我使用 Request ID "valid-request-id" 進行重試
        Then 請求應該被處理
        And 回應狀態碼應該是 200
        And 回應內容應包含處理結果
        And Request ID "valid-request-id" 應該從池中移除

    Scenario: 使用無效 Request ID 重試失敗
        Given 請求池中不存在 Request ID "invalid-request-id"
        When 我使用 Request ID "invalid-request-id" 進行重試
        Then 回應狀態碼應該是 404
        And 回應應包含 "Request ID not found" 訊息

    Scenario: 重試已完成的請求
        Given 系統中存在已完成的請求 "completed-request-id"
        When 我使用 Request ID "completed-request-id" 進行重試
        Then 回應狀態碼應該是 200
        And 回應內容應包含之前的處理結果

    Scenario: 重試仍在等待中的請求（系統忙碌）
        Given 請求池中存在 Request ID "waiting-request-id"
        And 系統已達到處理限制
        When 我使用 Request ID "waiting-request-id" 進行重試
        Then 回應狀態碼應該是 429
        And 回應應包含 "Still pending" 訊息