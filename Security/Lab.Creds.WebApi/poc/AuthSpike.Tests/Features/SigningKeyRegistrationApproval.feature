Feature: 簽章金鑰登錄申請與核准
  依 13 單驗收項目：Client 的請求簽章公鑰以申請與核准流程加入信任名單，與 mTLS 憑證分開管理、分開核准；
  待核准與已拒絕的簽章金鑰不能通過簽章驗證；核准只在申請它的 Client 名下生效；
  申請不得含私鑰；非管理員不能核准或拒絕。
  簽章金鑰申請端點與欄位為 lab 暫定值，待使用者確認；本 lab 不實作 Gateway。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 簽章金鑰情境中 "orders-client" 以 mTLS 取得 Token

  Scenario: 申請中的簽章金鑰不能通過簽章驗證
    When 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    Then 簽章金鑰申請回應為 202
    And 管理員查詢簽章金鑰申請狀態為 "待核准"
    When 以申請中的簽章金鑰送出建立訂單請求
    Then 簽章金鑰建立訂單回應為 401
    And 13 單第 1 項驗收已勾選

  Scenario: 管理員核准簽章金鑰後，該金鑰簽署的合法請求可通過驗證
    Given 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    When 以 "管理員憑證" 身分核准簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 200
    And 管理員查詢簽章金鑰申請狀態為 "已核准"
    When 以申請中的簽章金鑰送出建立訂單請求
    Then 簽章金鑰建立訂單回應為 201
    And 13 單第 2 項驗收已勾選

  Scenario: 核准簽章金鑰不使待核准的憑證生效
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    And 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    When 以 "管理員憑證" 身分核准簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 200
    When 以 "待核准憑證" 的 mTLS 憑證要求 "orders-client" 的 Token
    Then Token 要求未核發
    And 13 單第 3 項驗收已勾選

  @isolated
  Scenario: 核准憑證不使待核准的簽章金鑰生效
    Given 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    And 為 "orders-client" 提交只含公開憑證的登錄申請
    When 以 "管理員憑證" 身分核准該申請
    Then 管理員操作回應為 200
    When 以 "待核准憑證" 的 mTLS 憑證要求 "orders-client" 的 Token
    Then Token 要求已核發
    When 以申請中的簽章金鑰送出建立訂單請求
    Then 簽章金鑰建立訂單回應為 401
    And 13 單第 3 項驗收已勾選

  Scenario: 已拒絕的簽章金鑰不生效，且留下可查詢的拒絕結果
    Given 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    When 以 "管理員憑證" 身分拒絕簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 200
    And 管理員查詢簽章金鑰申請狀態為 "已拒絕"
    When 以申請中的簽章金鑰送出建立訂單請求
    Then 簽章金鑰建立訂單回應為 401
    When 以 "管理員憑證" 身分核准簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 409
    And 管理員查詢簽章金鑰申請狀態為 "已拒絕"
    And 13 單第 4 項驗收已勾選

  Scenario: 核准的簽章金鑰只在申請它的 Client 名下生效
    Given 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    When 以 "管理員憑證" 身分核准簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 200
    When 以 "orders-partner-client" 的 Token 送出以申請中簽章金鑰簽署的建立訂單請求
    Then 簽章金鑰建立訂單回應為 401
    And 13 單第 5 項驗收已勾選

  Scenario: 申請內容含私鑰時被拒絕
    When 為 "orders-client" 提交含私鑰的簽章金鑰申請
    Then 簽章金鑰申請回應為 400
    And 13 單第 6 項驗收已勾選

  Scenario Outline: 非管理員不能核准或拒絕簽章金鑰申請
    Given 為 "orders-client" 提交只含公開金鑰的簽章金鑰申請
    When 以 "<憑證>" 身分核准簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 401
    When 以 "<憑證>" 身分拒絕簽章金鑰申請
    Then 簽章金鑰管理員操作回應為 401
    And 管理員查詢簽章金鑰申請狀態為 "待核准"
    And 13 單第 6 項驗收已勾選

    Examples:
      | 憑證       |
      | 無憑證     |
      | 未登錄憑證 |
      | Client 憑證 |

  @record
  Scenario: 13 單的實作紀錄已標註簽章金鑰申請端點與欄位為 lab 暫定值
    Then 13 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 13 單的實作紀錄包含 "簽章金鑰申請"
    And 13 單第 7 項驗收已勾選

  @record
  Scenario: 13 單的實作紀錄說明可重現驗證方式
    Then 13 單的實作紀錄包含 "dotnet test"
    And 13 單的實作紀錄包含 "SigningKeyRegistrationApproval"
    And 13 單第 7 項驗收已勾選
