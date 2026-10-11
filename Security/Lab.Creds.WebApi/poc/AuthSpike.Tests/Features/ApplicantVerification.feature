Feature: 憑證登錄申請的申請者驗證與持有證明
  依 17 單驗收項目：憑證登錄申請只接受已登錄且有效的 Client 以現有已核准憑證經 mTLS 驗證後，為自己提出；
  申請者必須證明持有新憑證的私鑰，證明失敗或未驗證者不建立待核准紀錄；待核准紀錄帶已驗證的申請者身分；
  撤銷、停用或已退役憑證的 Client 不能申請；失敗回應不洩漏他人申請內容，也不讓未驗證者區分 Client 是否存在。
  持有證明的表示方式與申請欄位為 lab 暫定值，待使用者確認；全新 Client 的首次登錄不在本票範圍。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario Outline: 未驗證者送出登錄申請被拒絕且不建立待核准紀錄
    Given 記錄 "orders-client" 已受理的登錄申請稽核數
    When 以 "<身分>" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 申請者驗證回應為 401
    And "orders-client" 已受理的登錄申請稽核數不變
    And 17 單第 1 項驗收已勾選

    Examples:
      | 身分       |
      | 無憑證     |
      | 未登錄憑證 |

  Scenario: 已登錄 Client 替其他 Client 提出申請被拒絕且對方的待核准清單不變
    Given 為 "orders-client" 已有待核准的新憑證登錄申請
    And 記錄 "orders-client" 已受理的登錄申請稽核數
    When 以 "orders-partner-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 申請者驗證回應為 403
    And 失敗回應不含既有待核准申請的識別
    And "orders-client" 已受理的登錄申請稽核數不變
    And 17 單第 2 項驗收已勾選

  Scenario Outline: 無法證明持有新私鑰的申請被拒絕且不建立待核准紀錄
    Given 記錄 "orders-client" 已受理的登錄申請稽核數
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "<證明>" 的新憑證登錄申請
    Then 申請者驗證回應為 400
    And "orders-client" 已受理的登錄申請稽核數不變
    And 17 單第 3 項驗收已勾選

    Examples:
      | 證明     |
      | 缺漏     |
      | 他人私鑰 |

  Scenario: 已登錄 Client 以現有憑證為自己申請並證明持有新私鑰時建立帶已驗證身分的待核准紀錄
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 申請者驗證回應為 202
    And 管理員查詢待核准申請的已驗證申請者為 "orders-client"
    And 17 單第 4 項驗收已勾選

  @isolated
  Scenario: 原始憑證已撤銷的 Client 不能提出申請
    Given 管理員撤銷 "orders-client" 的原始憑證
    And 記錄 "orders-client" 已受理的登錄申請稽核數
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 申請者驗證回應為 401
    And "orders-client" 已受理的登錄申請稽核數不變
    And 17 單第 5 項驗收已勾選

  @isolated
  Scenario: 停用的 Client 不能提出申請
    Given 管理員停用 "orders-client"
    And 記錄 "orders-client" 已受理的登錄申請稽核數
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 申請者驗證回應為 401
    And "orders-client" 已受理的登錄申請稽核數不變
    And 17 單第 5 項驗收已勾選

  @isolated
  Scenario: 原始憑證已退役的 Client 不能提出申請
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And 管理員退役 "orders-client" 的原始憑證
    And 記錄 "orders-client" 已受理的登錄申請稽核數
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 申請者驗證回應為 401
    And "orders-client" 已受理的登錄申請稽核數不變
    And 17 單第 5 項驗收已勾選

  Scenario: 未驗證者無法區分 Client 不存在與身分驗證失敗
    When 無憑證分別為 "no-such-client" 與 "orders-client" 提出持有證明為 "有效" 的新憑證登錄申請
    Then 未驗證的兩次申請回應狀態與內容相同
    And 17 單第 6 項驗收已勾選

  @record
  Scenario: 17 單的首次登錄範圍已在實作紀錄說明
    Then 17 單的實作紀錄包含 "首次登錄"
    And 17 單的實作紀錄包含 "不在本票範圍"
    And 17 單第 7 項驗收已勾選

  @record
  Scenario: 17 單的實作紀錄已標註持有證明為 lab 暫定值並說明可重現驗證方式
    Then 17 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 17 單的實作紀錄包含 "dotnet test"
    And 17 單的實作紀錄包含 "ApplicantVerification"
    And 17 單第 8 項驗收已勾選
