Feature: 待核准申請的逾期處理
  依 19 單的延伸：待核准申請自提出起有逾期時間（lab 暫定 24 小時，待使用者確認）。
  逾期的申請視為已過期：不能再被核准或拒絕、不再佔用 Client 的待核准額度，Client 可再提出申請；管理員查詢可看到過期狀態。
  逾期以可注入的時鐘判定，情境不真的等待；既有核准、拒絕與上限行為不放寬。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @isolated
  Scenario: 逾期後的待核准憑證申請不能再被核准，管理員查詢顯示為已過期
    Given 逾期情境中為 "orders-client" 以 "憑證" 提出 1 筆待核准申請
    When 逾期情境時鐘推進 1440 分鐘
    And 逾期情境中管理員嘗試核准第 1 筆申請
    Then 逾期情境回應為 409
    And 逾期情境錯誤為 "request_expired"
    And 逾期情境中管理員查詢第 1 筆申請狀態為 "expired"

  @isolated
  Scenario Outline: 逾期後的申請無論類型或動作都不能被核准或拒絕
    Given 逾期情境中為 "orders-client" 以 "<類型>" 提出 1 筆待核准申請
    When 逾期情境時鐘推進 1440 分鐘
    And 逾期情境中管理員嘗試"<動作>"第 1 筆申請
    Then 逾期情境回應為 409
    And 逾期情境錯誤為 "request_expired"
    And 逾期情境中管理員查詢第 1 筆申請狀態為 "expired"

    Examples:
      | 類型     | 動作 |
      | 憑證     | 核准 |
      | 憑證     | 拒絕 |
      | 簽章金鑰 | 核准 |
      | 簽章金鑰 | 拒絕 |

  @isolated
  Scenario: 逾期的申請不再佔用額度，Client 可再提出新申請
    Given 逾期情境中為 "orders-client" 以 "憑證" 提出 5 筆待核准申請
    When 逾期情境中以 "orders-client" 身分為 "orders-client" 再提出一筆新 "憑證" 登錄申請
    Then 逾期情境回應為 429
    When 逾期情境時鐘推進 1440 分鐘
    And 逾期情境中以 "orders-client" 身分為 "orders-client" 再提出一筆新 "憑證" 登錄申請
    Then 逾期情境回應為 202

  @isolated
  Scenario: 未達逾期時間的待核准申請仍可被核准
    Given 逾期情境中為 "orders-client" 以 "憑證" 提出 1 筆待核准申請
    When 逾期情境時鐘推進 1439 分鐘
    And 逾期情境中管理員嘗試核准第 1 筆申請
    Then 逾期情境回應為 200
    And 逾期情境中管理員查詢第 1 筆申請狀態為 "approved"

  @record
  Scenario: 19 單的實作紀錄已更新逾期處理並標註待使用者確認
    Then 19 單的實作紀錄包含 "逾期處理"
    And 19 單的實作紀錄包含 "request_expired"
    And 19 單的實作紀錄包含 "lab 暫定、待使用者確認"
