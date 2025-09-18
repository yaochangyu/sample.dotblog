Feature: 請求狀態查詢
    作為一個 API 使用者
    我希望能夠查詢請求的處理狀態
    以便了解請求的當前進度

    Background: 系統初始狀態
        Given 系統速率限制為每分鐘 2 個請求

    Scenario: 查詢等待中請求的狀態
        Given 請求池中存在 Request ID "pending-request-id"
        And 該請求狀態為 "Pending"
        When 我查詢 Request ID "pending-request-id" 的狀態
        Then 回應狀態碼應該是 200
        And 回應應包含狀態 "Pending"
        And 回應應包含創建時間
        And 回應應包含預估處理時間

    Scenario: 查詢處理中請求的狀態
        Given 系統中存在 Request ID "processing-request-id"
        And 該請求狀態為 "Processing"
        When 我查詢 Request ID "processing-request-id" 的狀態
        Then 回應狀態碼應該是 200
        And 回應應包含狀態 "Processing"

    Scenario: 查詢已完成請求的狀態
        Given 系統中存在 Request ID "completed-request-id"
        And 該請求狀態為 "Completed"
        When 我查詢 Request ID "completed-request-id" 的狀態
        Then 回應狀態碼應該是 200
        And 回應應包含狀態 "Completed"
        And 回應應包含完成時間

    Scenario: 查詢不存在請求的狀態
        Given 系統中不存在 Request ID "nonexistent-request-id"
        When 我查詢 Request ID "nonexistent-request-id" 的狀態
        Then 回應狀態碼應該是 404
        And 回應應包含 "Request ID not found" 訊息