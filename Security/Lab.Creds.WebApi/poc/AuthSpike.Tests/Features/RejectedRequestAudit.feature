Feature: 被拒絕申請嘗試的稽核
  依 20 單驗收項目：申請端點上被拒絕的嘗試（身分未驗證、持有證明失敗、超過待核准上限、被撤銷或停用的 Client）
  都留下標為「未驗證」的管理操作稽核紀錄；紀錄不含私鑰與原始 Token，不把宣稱的 Client 身分記成已驗證；
  失敗回應不洩漏他人申請內容；稽核寫入失敗時申請不建立並明確回報服務錯誤。
  紀錄欄位與保存規則沿用 15 單的管理操作稽核；被拒絕嘗試的規則與數值為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario Outline: 身分未驗證的申請被拒絕並留下標為未驗證的紀錄
    When 20 單情境中以 "<身分>" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 401
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 401，原因為 "applicant_authentication_required"

    Examples:
      | 身分       |
      | 無憑證     |
      | 未登錄憑證 |

  Scenario Outline: 持有證明失敗的申請被拒絕並留下標為未驗證的紀錄
    When 20 單情境中以 "orders-client" 身分為 "orders-client" 提出持有證明為 "<證明>" 的新憑證登錄申請
    Then 20 單情境的申請回應為 400
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 400，原因為 "possession_proof_invalid"

    Examples:
      | 證明     |
      | 缺漏     |
      | 他人私鑰 |

  @isolated
  Scenario: 超過待核准上限的申請被拒絕並留下標為未驗證的紀錄
    Given 為 "orders-client" 已提出 5 筆待核准的憑證登錄申請
    When 20 單情境中以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 429
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 429，原因為 "pending_request_limit_reached"

  @isolated
  Scenario: 已撤銷憑證的 Client 提出的申請被拒絕並留下標為未驗證的紀錄
    Given 管理員撤銷 "orders-client" 的原始憑證
    When 20 單情境中以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 401
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 401，原因為 "applicant_authentication_required"

  @isolated
  Scenario: 已停用 Client 提出的申請被拒絕並留下標為未驗證的紀錄
    Given 管理員停用 "orders-client"
    When 20 單情境中以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 401
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 401，原因為 "applicant_authentication_required"

  Scenario: 未驗證的申請即使宣稱他人 Client 身分，紀錄仍標為未驗證且不記為已驗證
    When 20 單情境中以 "未登錄憑證" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 401
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 401，原因為 "applicant_authentication_required"
    And 20 單情境最新的申請稽核的操作者指紋為未登錄憑證

  Scenario: 替他人提出的申請被拒絕，失敗回應與稽核不洩漏對方的申請內容
    Given 20 單情境中為 "orders-client" 已有待核准的憑證登錄申請
    When 20 單情境中以 "orders-partner-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 403
    And 20 單情境的回應不含既有待核准申請的識別或指紋
    And 20 單情境最新的申請稽核對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"，結果代碼為 403，原因為 "applicant_mismatch"

  @isolated
  Scenario: 稽核寫入失敗時被拒絕的申請不留下待核准紀錄並回報服務錯誤
    Given 管理操作稽核暫時無法寫入
    When 20 單情境中以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 503
    And 20 單情境的回應錯誤為 "audit_unavailable"
    When 20 單情境的管理操作稽核恢復可寫入
    Then 20 單情境中連續 5 筆有效的新憑證登錄申請皆被受理

  @isolated
  Scenario: 被拒絕嘗試的稽核紀錄不含私鑰或原始 Token
    Given 20 單情境中 "orders-client" 以原始憑證取得 Token
    When 20 單情境中以 "無憑證" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 20 單情境的申請回應為 401
    And 20 單情境的管理操作稽核不含私鑰、原始 Token 或憑證私有內容

  @record
  Scenario: 20 單的實作紀錄說明被拒絕嘗試的稽核範圍與 lab 暫定值
    Then 20 單的實作紀錄包含 "未驗證"
    And 20 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 20 單的實作紀錄包含 "RejectedRequestAudit"
    And 20 單第 5 項驗收已勾選
