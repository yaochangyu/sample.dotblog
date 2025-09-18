Feature: 請求提交和處理
    作為一個 API 使用者
    我希望能夠提交請求進行處理
    以便在系統負載控制下獲得服務

    Background: 系統初始狀態
        Given 系統速率限制為每分鐘 2 個請求
        And 請求池為空

    Scenario: 立即處理請求（未超過速率限制）
        Given 當前沒有正在處理的請求
        When 我提交一個請求 "test-request-1"
        Then 請求應該被立即處理
        And 回應狀態碼應該是 200
        And 回應內容應包含處理結果

    Scenario: 第二個請求也能立即處理
        Given 已有 1 個請求在處理中
        When 我提交一個請求 "test-request-2"
        Then 請求應該被立即處理
        And 回應狀態碼應該是 200

    Scenario: 超過速率限制的請求進入池中
        Given 已有 2 個請求在處理中
        When 我提交一個請求 "test-request-3"
        Then 請求應該被加入請求池
        And 回應狀態碼應該是 429
        And 回應應包含 Request ID
        And 回應應包含重試時間

    Scenario: 多個請求超過限制時依序進入池中
        Given 已有 2 個請求在處理中
        When 我提交請求 "request-1", "request-2", "request-3"
        Then 所有請求都應該被加入請求池
        And 每個請求都應該獲得唯一的 Request ID