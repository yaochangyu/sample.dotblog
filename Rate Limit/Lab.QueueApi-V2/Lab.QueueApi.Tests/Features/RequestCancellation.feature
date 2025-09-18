Feature: 請求取消機制
    作為一個 API 使用者
    我希望能夠取消等待中的請求
    以便在不需要結果時釋放系統資源

    Background: 系統初始狀態
        Given 系統速率限制為每分鐘 2 個請求

    Scenario: 成功取消等待中的請求
        Given 請求池中存在 Request ID "pending-cancel-request"
        And 該請求狀態為 "Pending"
        When 我取消 Request ID "pending-cancel-request"
        Then 回應狀態碼應該是 200
        And 回應應包含 "Request cancelled successfully" 訊息
        And Request ID "pending-cancel-request" 應該從池中移除
        And 該請求狀態應該變為 "Canceled"

    Scenario: 嘗試取消不存在的請求
        Given 系統中不存在 Request ID "nonexistent-request"
        When 我取消 Request ID "nonexistent-request"
        Then 回應狀態碼應該是 404
        And 回應應包含 "Request ID not found" 訊息

    Scenario: 嘗試取消正在處理中的請求
        Given 系統中存在 Request ID "processing-request"
        And 該請求狀態為 "Processing"
        When 我取消 Request ID "processing-request"
        Then 回應狀態碼應該是 409
        And 回應應包含 "Cannot cancel request in Processing state" 訊息

    Scenario: 嘗試取消已完成的請求
        Given 系統中存在 Request ID "completed-request"
        And 該請求狀態為 "Completed"
        When 我取消 Request ID "completed-request"
        Then 回應狀態碼應該是 409
        And 回應應包含 "Cannot cancel completed request" 訊息

    Scenario: 嘗試取消已失敗的請求
        Given 系統中存在 Request ID "failed-request"
        And 該請求狀態為 "Failed"
        When 我取消 Request ID "failed-request"
        Then 回應狀態碼應該是 409
        And 回應應包含 "Cannot cancel failed request" 訊息