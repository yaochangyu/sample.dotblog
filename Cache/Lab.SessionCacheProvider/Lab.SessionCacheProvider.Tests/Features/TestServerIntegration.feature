Feature: TestServer 整合測試

    使用 ASP.NET Core TestServer 驗證 SessionCacheProvider 在真實 HTTP 管線下的行為

    Scenario: 首次請求自動建立 SessionCacheId cookie
        Given 一個 TestServer 應用程式
        When 發送 GET 請求到 "/api/session/get?key=Name"
        Then Response 應包含 "SessionCacheId" cookie

    Scenario: 跨請求透過 cookie 取回先前設定的值
        Given 一個 TestServer 應用程式
        When 發送 POST 請求到 "/api/session/set" 並帶入 key "City" 值 "Taipei"
        And 帶著相同的 cookie 發送 GET 請求到 "/api/session/get?key=City"
        Then Response 的內容應為 "Taipei"

    Scenario: 不同 Session 之間資料互不干擾
        Given 一個 TestServer 應用程式
        When 發送 POST 請求到 "/api/session/set" 並帶入 key "Token" 值 "AAA"
        And 不帶 cookie 發送 GET 請求到 "/api/session/get?key=Token"
        Then Response 的內容應為空字串

    Scenario: 透過 CacheSession.Current 靜態存取設定與取得值
        Given 一個 TestServer 應用程式
        When 發送 POST 請求到 "/api/session/set-static" 並帶入 key "Lang" 值 "zh-TW"
        And 帶著相同的 cookie 發送 GET 請求到 "/api/session/get-static?key=Lang"
        Then Response 的內容應為 "zh-TW"
