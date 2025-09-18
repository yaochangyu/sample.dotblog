Feature: 背景處理機制
    作為一個系統管理員
    我希望系統能夠自動處理請求池中的請求
    以便確保請求得到適時處理和清理

    Background: 系統初始狀態
        Given 系統速率限制為每分鐘 2 個請求
        And 背景處理服務正在運行

    Scenario: 背景服務處理池中的請求
        Given 請求池中有 3 個等待的請求
        And 系統當前沒有處理中的請求
        When 背景服務檢查處理機會
        Then 應該從池中取得第一個請求進行處理
        And 該請求狀態應該變為 "Processing"
        And 池中剩餘請求數量應該減少 1

    Scenario: 背景服務在系統忙碌時等待
        Given 請求池中有 2 個等待的請求
        And 系統已有 2 個請求在處理中
        When 背景服務檢查處理機會
        Then 不應該處理任何池中的請求
        And 所有池中請求應該保持 "Pending" 狀態

    Scenario: 自動清理超時請求
        Given 請求池中有一個創建於 2 分鐘前的請求 "timeout-request"
        When 背景服務執行清理作業
        Then Request ID "timeout-request" 狀態應該變為 "UnProcessed"
        And Request ID "timeout-request" 應該從池中移除

    Scenario: 保留未超時的請求
        Given 請求池中有一個創建於 30 秒前的請求 "recent-request"
        When 背景服務執行清理作業
        Then Request ID "recent-request" 狀態應該保持 "Pending"
        And Request ID "recent-request" 應該仍在池中

    Scenario: 批量清理多個超時請求
        Given 請求池中有 5 個創建於 2 分鐘前的請求
        When 背景服務執行清理作業
        Then 所有 5 個請求狀態應該變為 "UnProcessed"
        And 所有 5 個請求應該從池中移除
        And 系統記憶體應該被釋放

    Scenario: 背景服務定期執行清理
        Given 背景服務已運行 30 秒
        When 系統時間達到下一個清理週期
        Then 背景服務應該自動執行清理作業
        And 清理作業應該檢查所有池中請求的超時狀態