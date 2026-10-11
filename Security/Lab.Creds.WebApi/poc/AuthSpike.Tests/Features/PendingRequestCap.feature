Feature: 每個 Client 的待核准申請數上限
  依 19 單驗收項目：每個 Client 的待核准申請（憑證申請與簽章金鑰申請合計）有上限；達到上限後新申請被明確拒絕，
  既有待核准申請不受影響；其他 Client 的額度不受影響；管理員核准或拒絕後額度釋放。
  上限數值與逾期處理為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @isolated
  Scenario: 同一 Client 達到上限後新申請被明確拒絕且既有待核准申請不受影響
    Given 為 "orders-client" 已提出 5 筆待核准的憑證登錄申請
    When 以 "orders-client" 身分為 "orders-client" 再提出一筆新憑證登錄申請
    Then 待核准上限測試回應為 429
    And 待核准上限測試錯誤為 "pending_request_limit_reached"
    And 管理員查詢第 1 筆待核准申請狀態為 "pending"
    And 管理員查詢第 5 筆待核准申請狀態為 "pending"
    And 19 單第 1 項驗收已勾選

  @isolated
  Scenario: 憑證與簽章金鑰的待核准申請合計計算上限
    Given 為 "orders-client" 已提出 3 筆待核准的憑證登錄申請
    And 為 "orders-client" 已提出 2 筆待核准的簽章金鑰登錄申請
    When 以 "orders-client" 身分為 "orders-client" 再提出一筆新憑證登錄申請
    Then 待核准上限測試回應為 429
    And 待核准上限測試錯誤為 "pending_request_limit_reached"
    When 以 "orders-client" 身分為 "orders-client" 再提出一筆新簽章金鑰登錄申請
    Then 待核准上限測試回應為 429
    And 待核准上限測試錯誤為 "pending_request_limit_reached"
    And 19 單第 2 項驗收已勾選

  @isolated
  Scenario: 一個 Client 達到上限不影響其他 Client 提出申請
    Given 為 "orders-client" 已提出 5 筆待核准的憑證登錄申請
    When 以 "orders-client" 身分為 "orders-client" 再提出一筆新憑證登錄申請
    Then 待核准上限測試回應為 429
    When 以 "orders-partner-client" 身分為 "orders-partner-client" 再提出一筆新憑證登錄申請
    Then 待核准上限測試回應為 202
    And 19 單第 3 項驗收已勾選

  @isolated
  Scenario Outline: 管理員核准或拒絕後額度釋放，Client 可再提出新申請
    Given 為 "orders-client" 已提出 5 筆待核准的<類型>登錄申請
    When 以 "orders-client" 身分為 "orders-client" 再提出一筆新<類型>登錄申請
    Then 待核准上限測試回應為 429
    When 管理員<動作>第 1 筆待核准申請
    When 以 "orders-client" 身分為 "orders-client" 再提出一筆新<類型>登錄申請
    Then 待核准上限測試回應為 202
    And 19 單第 4 項驗收已勾選

    Examples:
      | 類型     | 動作 |
      | 憑證     | 核准 |
      | 憑證     | 拒絕 |
      | 簽章金鑰 | 核准 |
      | 簽章金鑰 | 拒絕 |

  @record
  Scenario: 19 單的上限與逾期處理已標註為 lab 暫定值待使用者確認
    Then 19 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 19 單的實作紀錄包含 "逾期處理"
    And 19 單第 5 項驗收已勾選

  @record
  Scenario: 19 單每個驗收項目都對應至少一個 BDD Scenario
    Then 19 單的實作紀錄包含 "PendingRequestCap"
    And 19 單第 6 項驗收已勾選
