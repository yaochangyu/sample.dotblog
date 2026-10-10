Feature: 憑證登錄申請與核准／拒絕
  依 12 單驗收項目：Client 的用戶端憑證先以公開憑證提交申請，經管理員核准後才進入信任名單並生效；
  待核准與已拒絕的憑證不能取得 Token；申請只接受公開憑證；非管理員與申請者本人不能核准或拒絕；
  核准某 Client 的申請不改變其他 Client 的憑證與權限。
  申請的傳遞管道不在本單範圍；申請端點與申請欄位為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario: 提交申請後為待核准且無法取得 Token
    When 為 "orders-client" 提交只含公開憑證的登錄申請
    Then 登錄申請回應為 202
    And 管理員查詢申請狀態為 "待核准"
    When 以 "待核准憑證" 的 mTLS 憑證要求 "orders-client" 的 Token
    Then Token 要求未核發
    And 12 單第 1 項驗收已勾選

  Scenario: 管理員核准後憑證進入信任名單並可取得綁定該憑證的 Token
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    When 以 "管理員憑證" 身分核准該申請
    Then 管理員操作回應為 200
    And 管理員查詢申請狀態為 "已核准"
    And 信任名單中 "orders-client" 包含待核准憑證指紋
    When 以 "待核准憑證" 的 mTLS 憑證要求 "orders-client" 的 Token
    Then Token 要求已核發
    When 以 "待核准憑證" 的 mTLS 憑證持該 Token 送出建立訂單請求
    Then 建立訂單回應為 201
    And 12 單第 2 項驗收已勾選

  Scenario: 管理員拒絕後憑證不生效且留下可查詢的拒絕結果
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    And 記錄信任名單基準
    When 以 "管理員憑證" 身分拒絕該申請
    Then 管理員操作回應為 200
    And 管理員查詢申請狀態為 "已拒絕"
    And 信任名單與核准前基準相同
    When 以 "待核准憑證" 的 mTLS 憑證要求 "orders-client" 的 Token
    Then Token 要求未核發
    When 以 "管理員憑證" 身分核准該申請
    Then 管理員操作回應為 409
    And 管理員查詢申請狀態為 "已拒絕"
    And 12 單第 3 項驗收已勾選

  Scenario: 核准某 Client 的申請不改變其他 Client 的憑證與權限
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    And 記錄信任名單基準
    When 以 "管理員憑證" 身分核准該申請
    Then 管理員操作回應為 200
    And 信任名單中除 "orders-client" 外與核准前基準相同
    And "orders-partner-client" 的 Client 憑證仍可取得 Token
    And "billing-client" 的 Client 憑證仍可取得 Token
    And 12 單第 4 項驗收已勾選

  Scenario: 含私鑰的申請被拒絕
    When 為 "orders-client" 提交含私鑰的登錄申請
    Then 登錄申請回應為 400
    And 12 單第 5 項驗收已勾選

  Scenario Outline: 非管理員不能核准或拒絕申請，申請者本人也不能自行核准
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    When 以 "<憑證>" 身分核准該申請
    Then 管理員操作回應為 401
    When 以 "<憑證>" 身分拒絕該申請
    Then 管理員操作回應為 401
    And 管理員查詢申請狀態為 "待核准"
    And 信任名單不包含待核准憑證指紋
    And 12 單第 6 項驗收已勾選

    Examples:
      | 憑證       |
      | 無憑證     |
      | 未登錄憑證 |
      | Client 憑證 |
      | 待核准憑證 |

  @record
  Scenario: 12 單的實作紀錄已標註申請欄位與端點為 lab 暫定值
    Then 12 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 12 單的實作紀錄包含 "申請內容欄位"
    And 12 單第 7 項驗收已勾選

  @record
  Scenario: 12 單的實作紀錄說明可重現驗證方式
    Then 12 單的實作紀錄包含 "dotnet test"
    And 12 單的實作紀錄包含 "CertificateRegistrationApproval"
    And 12 單第 8 項驗收已勾選
