Feature: 使用者 Token 簽發與使用者驗證路徑（02 單）
  授權伺服器為測試使用者簽發使用者 Token，帶獨立 audience 與呼叫者類型；建立訂單 API 接受有效的使用者 Token，
  產出類型為使用者的已驗證呼叫者，並以該使用者身分提交業務操作。偽造、過期、簽發者不符或已停用使用者的 Token 一律被拒絕且無業務副作用。
  使用者路徑不要求 mTLS 與請求簽章；服務路徑的保護不因此改變。本 lab 以測試使用者帳號代替真實登入流程。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @user-caller @isolated
  Scenario: AC-D01 授權伺服器以測試使用者帳號簽發含獨立 audience 與呼叫者類型的使用者 Token
    Given 使用者呼叫端以測試使用者帳號取得使用者 Token
    When 查詢使用者 Token 的內省結果
    Then 使用者 Token 內省結果為 active，目標 API 為 "orders-api-user"，呼叫者類型為 "user"，使用者為 "test-user-1"

  @user-caller @isolated
  Scenario: AC-D01 有效的使用者 Token 不附用戶端憑證與簽章即可建立訂單，業務操作以該使用者身分提交
    Given 使用者呼叫端以測試使用者帳號取得使用者 Token
    When 使用者呼叫端不附用戶端憑證與簽章，送出建立訂單請求
    Then 使用者呼叫端最近一次請求回應 201
    And 使用者呼叫端建立的訂單以使用者 "test-user-1" 身分提交
    And 稽核情境中業務 API 建立訂單總數為 1

  @user-caller @isolated
  Scenario: AC-D06 授權伺服器以錯誤密碼拒絕使用者 Token 要求
    When 使用者呼叫端以測試使用者帳號與錯誤密碼要求使用者 Token
    Then 授權伺服器拒絕使用者 Token 要求，錯誤代碼為 "invalid_grant"

  @user-caller @isolated
  Scenario: AC-D06 偽造的使用者 Token 被拒絕且無業務副作用
    When 使用者呼叫端以偽造的使用者 Token 送出建立訂單請求
    Then 使用者呼叫端最近一次請求回應 401
    And 稽核情境中業務 API 建立訂單總數為 0

  @user-caller @short-lifetime
  Scenario: AC-D06 過期的使用者 Token 被拒絕且無業務副作用
    Given 使用者呼叫端以測試使用者帳號取得使用者 Token
    When 等待 Token 到期
    And 使用者呼叫端不附用戶端憑證與簽章，送出建立訂單請求
    Then 使用者呼叫端最近一次請求回應 401
    And 稽核情境中業務 API 建立訂單總數為 0

  @user-caller @isolated
  Scenario: AC-D06 簽發者不符的使用者 Token 被拒絕且無業務副作用
    Given 另一個環境 "other-env" 已由使用者取得使用者 Token
    When 使用者呼叫端不附用戶端憑證與簽章，送出建立訂單請求
    Then 使用者呼叫端最近一次請求回應 401
    And 稽核情境中業務 API 建立訂單總數為 0

  @user-caller @isolated
  Scenario: 服務 Token 不能以使用者路徑通過，audience 與呼叫者類型不符被拒絕
    Given 服務 "orders-client" 已以憑證取得服務 Token
    When 使用者呼叫端不附用戶端憑證與簽章，送出建立訂單請求
    Then 使用者呼叫端最近一次請求回應 401
    And 稽核情境中業務 API 建立訂單總數為 0

  @user-caller @isolated
  Scenario: 使用者有效狀態為停用時，既有使用者 Token 的建立訂單請求被拒絕且無業務副作用
    Given 使用者呼叫端以測試使用者帳號取得使用者 Token
    When 測試使用者被停用
    And 使用者呼叫端不附用戶端憑證與簽章，送出建立訂單請求
    Then 使用者呼叫端最近一次請求回應 401
    And 稽核情境中業務 API 建立訂單總數為 0

  @record
  Scenario: 02 單驗收項目已逐項勾選且狀態為 resolved
    Then 02 單第 1 項驗收已勾選
    And 02 單第 2 項驗收已勾選
    And 02 單第 3 項驗收已勾選
    And 02 單第 4 項驗收已勾選
    And 02 單第 5 項驗收已勾選
    And 02 單第 6 項驗收已勾選
    And 02 單第 7 項驗收已勾選
    And 02 單第 8 項驗收已勾選
    And 02 單的狀態為 resolved
