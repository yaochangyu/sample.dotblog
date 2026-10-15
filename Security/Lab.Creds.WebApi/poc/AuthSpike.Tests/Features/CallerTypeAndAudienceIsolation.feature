Feature: 呼叫者類型與 audience 隔離（03 單）
  服務 Token 與使用者 Token 互不通用：使用者持服務 Token、服務持使用者 Token、或 Token 的 audience 與呼叫者類型不符，一律被拒絕。
  呼叫者類型由已驗證的 Token 與出示的用戶端憑證決定，不採信請求內容的宣稱。服務路徑的保護不因使用者路徑存在而降級。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @caller-isolation @isolated
  Scenario: AC-D03 使用者持服務 Token 走使用者驗證時被拒絕且無業務副作用
    Given 持有服務 "orders-client" 的服務 Token（03 單）
    When 持服務 Token 的呼叫端不附用戶端憑證，送出建立訂單請求（03 單）
    Then 03 單呼叫端最近一次請求回應 401
    And 03 單建立訂單總數為 0
    And 03 單最近一次稽核紀錄結果為 "rejected"
    And 03 單最近一次稽核紀錄沒有已驗證 Client

  @caller-isolation @isolated
  Scenario: AC-D04 服務持使用者 Token 走服務驗證時被拒絕且無業務副作用
    Given 持有使用者 Token（03 單）
    When 服務 "orders-client" 以憑證持使用者 Token 送出已簽章的建立訂單請求（03 單）
    Then 03 單呼叫端最近一次請求回應 401
    And 03 單建立訂單總數為 0
    And 03 單最近一次稽核紀錄結果為 "rejected"
    And 03 單最近一次稽核紀錄沒有已驗證 Client

  @caller-isolation @isolated
  Scenario: AC-D05 服務 Token 的 audience 不屬於使用者 audience，以使用者身分內省為 inactive
    Given 持有服務 "orders-client" 的服務 Token（03 單）
    When 以使用者查證身分內省服務 Token（03 單）
    Then 03 單內省結果為 inactive

  @caller-isolation @isolated
  Scenario: AC-D05 使用者 Token 的 audience 不屬於業務 API，以服務查證身分內省為 inactive
    Given 持有使用者 Token（03 單）
    When 以業務 API 服務身分內省使用者 Token（03 單）
    Then 03 單內省結果為 inactive

  @caller-isolation @isolated
  Scenario: AC-D05 audience 與呼叫者類型相符的使用者 Token 仍被接受（對照組）
    Given 持有使用者 Token（03 單）
    When 使用者呼叫端不附用戶端憑證，送出建立訂單請求（03 單）
    Then 03 單呼叫端最近一次請求回應 201
    And 03 單建立訂單總數為 1

  @caller-isolation @isolated
  Scenario: 請求宣稱的呼叫者類型與 Client 身分不被採信，業務以已驗證的使用者身分提交
    Given 持有使用者 Token（03 單）
    When 使用者呼叫端不附用戶端憑證，送出建立訂單請求，Body 與標頭宣稱為服務 "orders-client"（03 單）
    Then 03 單呼叫端最近一次請求回應 201
    And 03 單回應 clientId 為 "test-user-1"
    And 03 單最近一次稽核紀錄結果為 "accepted"

  @caller-isolation @isolated
  Scenario: 驗證失敗的請求其宣稱的 Client 身分只標為未驗證
    Given 持有服務 "orders-client" 的服務 Token（03 單）
    When 持服務 Token 的呼叫端不附用戶端憑證，送出建立訂單請求，並附 X-Client-Id 標頭 "orders-client"（03 單）
    Then 03 單呼叫端最近一次請求回應 401
    And 03 單最近一次稽核紀錄沒有已驗證 Client

  @caller-isolation @isolated
  Scenario: AC-D02 服務持服務 Token 出示憑證卻未簽章時仍被拒絕，不因使用者路徑存在而降級
    Given 持有服務 "orders-client" 的服務 Token（03 單）
    When 服務 "orders-client" 以憑證持服務 Token 送出未簽章的建立訂單請求（03 單）
    Then 03 單呼叫端最近一次請求回應 401
    And 03 單建立訂單總數為 0
    And 03 單最近一次稽核紀錄結果為 "rejected"

  @caller-isolation @isolated
  Scenario: AC-D02 服務持服務 Token 以憑證與正確簽章送出仍被接受（服務路徑未受影響）
    Given 持有服務 "orders-client" 的服務 Token（03 單）
    When 服務 "orders-client" 以憑證持服務 Token 送出已簽章的建立訂單請求（03 單）
    Then 03 單呼叫端最近一次請求回應 201
    And 03 單建立訂單總數為 1

  @record
  Scenario: 03 單驗收項目已逐項勾選且狀態為 resolved
    Then 呼叫者隔離票第 1 項驗收已勾選
    And 呼叫者隔離票第 2 項驗收已勾選
    And 呼叫者隔離票第 3 項驗收已勾選
    And 呼叫者隔離票第 4 項驗收已勾選
    And 呼叫者隔離票第 5 項驗收已勾選
    And 呼叫者隔離票第 6 項驗收已勾選
    And 呼叫者隔離票的狀態為 resolved
