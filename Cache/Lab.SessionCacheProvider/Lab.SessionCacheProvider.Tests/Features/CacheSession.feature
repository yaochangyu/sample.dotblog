Feature: CacheSession

    Scenario: 透過 CacheSession.Current 設定與取得值
        Given 已設定 CacheSession
        When 透過 CacheSession.Current 設定 key "UserName" 的值為 "John"
        Then 透過 CacheSession.Current 取得 key "UserName" 的值應為 "John"

    Scenario: 取得不存在的 key 回傳 null
        Given 已設定 CacheSession
        Then 透過 CacheSession.Current 取得 key "NonExisting" 的值應為 null

    Scenario: 透過 CacheSession.Current 移除值
        Given 已設定 CacheSession
        And 透過 CacheSession.Current 設定 key "UserName" 的值為 "John"
        When 透過 CacheSession.Current 移除 key "UserName"
        Then 透過 CacheSession.Current 取得 key "UserName" 的值應為 null
