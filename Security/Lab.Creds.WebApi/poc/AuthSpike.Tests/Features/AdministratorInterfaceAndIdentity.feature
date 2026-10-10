Feature: 管理介面與管理員身分驗證
  依 11 單驗收項目，授權伺服器的管理介面只接受以專屬管理員 mTLS 憑證驗證的呼叫；
  未經管理員驗證的呼叫被拒絕且信任名單不變；管理憑證不能取得業務 Token 或呼叫業務 API，
  Client 憑證不能呼叫管理介面；管理介面與業務 API 的驗證結果各自獨立。
  管理員憑證的登錄方式、管理介面路徑與回應格式皆為 lab 暫定值，待使用者確認；本 lab 不實作 Gateway。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 11 單的實作紀錄可讀取

  Scenario: 管理員憑證與 Client 憑證分屬不同身分
    Then 管理員憑證的指紋與 "orders-client" 的 mTLS 憑證指紋不同
    And 管理員憑證不在任何 Client 的信任登錄中
    And 11 單第 1 項驗收已勾選

  Scenario: 已驗證管理員可讀取信任名單
    When 以 "管理員憑證" 身分讀取管理介面的信任名單
    Then 管理介面回應為 200
    And 信任名單包含 "orders-client"
    And 信任名單中 "orders-client" 為 "啟用"
    And 11 單第 1 項驗收已勾選

  Scenario Outline: 未經管理員驗證的呼叫讀取信任名單被拒絕
    When 以 "<憑證>" 身分讀取管理介面的信任名單
    Then 管理介面回應為 401
    Examples:
      | 憑證       |
      | 無憑證     |
      | 未登錄憑證 |
      | Client 憑證 |

  Scenario Outline: 未經管理員驗證的呼叫停用 Client 被拒絕且信任名單不變
    Given 管理員已讀取信任名單作為基準
    When 以 "<憑證>" 身分呼叫管理介面停用 "orders-client"
    Then 管理介面回應為 401
    And 信任名單與基準相同
    And 信任名單中 "orders-client" 為 "啟用"
    And 11 單第 2 項驗收已勾選

    Examples:
      | 憑證       |
      | 無憑證     |
      | 未登錄憑證 |
      | Client 憑證 |

  @isolated
  Scenario: 管理員停用 Client 後信任名單與授權結果同步反映
    Given 管理員已讀取信任名單作為基準
    When 以 "管理員憑證" 身分呼叫管理介面停用 "orders-client"
    Then 管理介面回應為 200
    And 信任名單中 "orders-client" 為 "停用"
    And 以 "Client 憑證" 的 mTLS 憑證向授權伺服器要求 Token 被拒絕

  Scenario: 管理憑證不能取得業務 Token
    When 以 "管理員憑證" 身分要求 "orders-client" 的 Token
    Then 管理員的 Token 要求未核發
    And 11 單第 3 項驗收已勾選

  Scenario: 管理憑證不能呼叫業務 API
    Given 管理介面情境中 "orders-client" 以 mTLS 取得 Token
    When 以 "管理員憑證" 身分持該 Token 送出建立訂單請求
    Then 業務 API 回應為 401
    And 業務 API 的訂單數未增加
    And 11 單第 3 項驗收已勾選

  Scenario: 管理介面與業務 API 的驗證結果各自獨立
    Given 管理介面情境中 "orders-client" 以 mTLS 取得 Token
    When 以 "Client 憑證" 身分持該 Token 送出建立訂單請求
    Then 業務 API 回應為 201
    When 以 "Client 憑證" 身分讀取管理介面的信任名單
    Then 管理介面回應為 401
    When 以 "管理員憑證" 身分讀取管理介面的信任名單
    Then 管理介面回應為 200
    When 以 "管理員憑證" 身分持該 Token 送出建立訂單請求
    Then 業務 API 回應為 401
    And 11 單第 4 項驗收已勾選

  @record
  Scenario: 管理員憑證的登錄方式與管理介面的 lab 暫定值已標註待確認
    Then 11 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 11 單的實作紀錄包含 "/admin/trust-list"
    And 11 單的實作紀錄包含 "管理員憑證的登錄方式"
    And 11 單第 6 項驗收已勾選

  @record
  Scenario: 11 單的實作紀錄說明可重現驗證方式
    Then 11 單的實作紀錄包含 "dotnet test"
    And 11 單的實作紀錄包含 "AdministratorInterfaceAndIdentity"
    And 11 單第 5 項驗收已勾選
