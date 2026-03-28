Feature: SessionCacheProvider

    Scenario: 無 Cookie 時自動建立新的 SessionId
        Given 一個沒有 session cookie 的 HTTP 請求
        When 存取 Session 屬性
        Then 應建立新的 session ID cookie
        And Session 屬性應回傳 SessionObject

    Scenario: 有 Cookie 時重用既有 SessionId
        Given 一個帶有 session cookie "abc123" 的 HTTP 請求
        When 存取 Session 屬性
        Then session ID 應為 "abc123"

    Scenario: 透過 SessionCacheProvider 存取值
        Given 一個沒有 session cookie 的 HTTP 請求
        When 透過 Session 屬性設定 key "Greeting" 的值為 "Hello"
        Then 透過 Session 屬性取得 key "Greeting" 的值應為 "Hello"
