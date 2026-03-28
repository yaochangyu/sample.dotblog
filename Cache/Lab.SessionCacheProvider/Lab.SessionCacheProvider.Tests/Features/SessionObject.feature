Feature: SessionObject

    Scenario: 透過索引器設定與取得值
        Given 一個 SessionObject 實例
        When 設定 key "UserName" 的值為 "John"
        Then key "UserName" 的值應為 "John"

    Scenario: 取得不存在的 key 回傳 null
        Given 一個 SessionObject 實例
        Then key "NonExisting" 的值應為 null

    Scenario: 設定 null 等同移除
        Given 一個 SessionObject 實例
        And 設定 key "UserName" 的值為 "John"
        When 設定 key "UserName" 的值為 null
        Then key "UserName" 的值應為 null

    Scenario: 透過 Remove 移除值
        Given 一個 SessionObject 實例
        And 設定 key "UserName" 的值為 "John"
        When 移除 key "UserName"
        Then key "UserName" 的值應為 null

    Scenario: 設定與取得強型別值
        Given 一個 SessionObject 實例
        When 設定 key "Age" 的整數值為 42
        Then key "Age" 的整數值應為 42

    Scenario: 覆寫既有的值
        Given 一個 SessionObject 實例
        And 設定 key "UserName" 的值為 "John"
        When 設定 key "UserName" 的值為 "Jane"
        Then key "UserName" 的值應為 "Jane"
