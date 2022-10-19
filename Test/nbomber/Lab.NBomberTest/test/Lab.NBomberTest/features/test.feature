Feature: test
Simple calculator for adding two numbers

    Scenario: 壓力測試
        Given 準備以下 Header 參數
            | Key       | Value  |
            | x-api-key | 123456 |
        Given 準備 HttpRequest 'GET', "http://test.k6.io"
        Then 執行測試