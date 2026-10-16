Feature: 依呼叫者類型授權（04 單）
  授權規則依呼叫者類型分開定義：使用者只能操作自己的業務資料範圍；服務維持被核准的操作與業務資料範圍；
  限定單一呼叫者類型的操作，由另一類型呼叫時被拒絕。服務的高權限不能代替使用者的權限限制。
  使用者資料範圍與服務的 scope 白名單為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @caller-authorization @isolated
  Scenario: AC-D07 使用者要求他人的使用者訂單時被資料範圍授權拒絕，且他人的操作不生效
    Given 使用者 "test-user-1" 取得使用者 Token（04 單）
    And 使用者 "test-user-2" 取得使用者 Token（04 單）
    And 使用者 "test-user-1" 建立訂單 "甲訂單"（04 單）
    When 使用者 "test-user-2" 以使用者 Token 查詢訂單 "甲訂單"（04 單）
    Then 04 單最近一次請求回應 404
    When 使用者 "test-user-2" 以使用者 Token 取消訂單 "甲訂單"（04 單）
    Then 04 單最近一次請求回應 404
    And 04 單訂單 "甲訂單" 由 "test-user-1" 持有的狀態為 "active"（04 單）

  @caller-authorization @isolated
  Scenario: AC-D07 使用者要求服務建立的訂單時被資料範圍授權拒絕
    Given 使用者 "test-user-1" 取得使用者 Token（04 單）
    And 服務 "orders-client" 以授權範圍 "orders.read orders.write" 取得服務 Token（04 單）
    And 服務 "orders-client" 以已簽章請求建立訂單 "服務訂單"（04 單）
    When 使用者 "test-user-1" 以使用者 Token 查詢訂單 "服務訂單"（04 單）
    Then 04 單最近一次請求回應 404

  @caller-authorization @isolated
  Scenario: AC-D07 使用者查詢自己的訂單仍回應 200（對照組）
    Given 使用者 "test-user-1" 取得使用者 Token（04 單）
    And 使用者 "test-user-1" 建立訂單 "甲訂單"（04 單）
    When 使用者 "test-user-1" 以使用者 Token 查詢訂單 "甲訂單"（04 單）
    Then 04 單最近一次請求回應 200

  @caller-authorization @isolated
  Scenario: AC-D08 服務以自身高權限執行使用者專屬的限制操作時依呼叫者類型規則被拒絕，且無資料外洩
    Given 服務 "orders-client" 以授權範圍 "orders.read orders.write" 取得服務 Token（04 單）
    And 使用者 "test-user-1" 取得使用者 Token（04 單）
    And 使用者 "test-user-1" 建立訂單 "使用者訂單"（04 單）
    When 服務 "orders-client" 以已簽章請求查詢使用者專屬訂單 "使用者訂單"（04 單）
    Then 04 單最近一次請求回應 403
    And 04 單最近一次回應不含訂單 "使用者訂單"（04 單）
    And 04 單訂單 "使用者訂單" 由 "test-user-1" 持有的狀態為 "active"（04 單）

  @caller-authorization @isolated
  Scenario: AC-D14 限定使用者的操作由服務呼叫者（僅持 orders.read）呼叫時被拒絕
    Given 服務 "orders-client" 以授權範圍 "orders.read" 取得服務 Token（04 單）
    And 使用者 "test-user-1" 取得使用者 Token（04 單）
    And 使用者 "test-user-1" 建立訂單 "使用者訂單"（04 單）
    When 服務 "orders-client" 以已簽章請求查詢使用者專屬訂單 "使用者訂單"（04 單）
    Then 04 單最近一次請求回應 403

  @caller-authorization @isolated
  Scenario: 服務維持被核准的操作與業務資料範圍，不因使用者路徑而擴大
    Given 服務 "orders-client" 以授權範圍 "orders.read orders.write" 取得服務 Token（04 單）
    And 服務 "orders-client" 以已簽章請求建立訂單 "服務訂單"（04 單）
    And 服務 "orders-client" 以授權範圍 "orders.read" 取得服務 Token（04 單）
    When 服務 "orders-client" 以已簽章請求查詢訂單 "服務訂單"（04 單）
    Then 04 單最近一次請求回應 200
    When 服務 "orders-client" 以已簽章請求查詢使用者專屬訂單 "服務訂單"（04 單）
    Then 04 單最近一次請求回應 403

  @record
  Scenario: 04 單驗收項目已逐項勾選、狀態為 resolved，且使用者資料範圍已標註待使用者確認
    Then 依呼叫者類型授權票第 1 項驗收已勾選
    And 依呼叫者類型授權票第 2 項驗收已勾選
    And 依呼叫者類型授權票第 3 項驗收已勾選
    And 依呼叫者類型授權票第 4 項驗收已勾選
    And 依呼叫者類型授權票第 5 項驗收已勾選
    And 依呼叫者類型授權票第 6 項驗收已勾選
    And 依呼叫者類型授權票的狀態為 resolved
    And 依呼叫者類型授權票的實作紀錄標註使用者資料範圍為待使用者確認
