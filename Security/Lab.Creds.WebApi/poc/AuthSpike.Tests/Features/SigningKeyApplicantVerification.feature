Feature: 簽章金鑰登錄申請的申請者驗證與持有證明
  依 18 單驗收項目：簽章金鑰登錄申請只接受已登錄且有效的 Client 以現有已核准憑證經 mTLS 驗證後，為自己提出；
  申請者必須以新金鑰的私鑰對申請內容簽章，證明失敗或未驗證者不建立待核准紀錄；待核准紀錄帶已驗證的申請者身分；
  撤銷、停用或已退役憑證的 Client 不能申請；簽章金鑰與憑證申請的驗證互不連動；失敗回應不洩漏他人申請內容。
  持有證明的簽章內容與申請欄位為 lab 暫定值，待使用者確認；全新 Client 的首次登錄不在本票範圍。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario Outline: 未驗證者送出簽章金鑰申請被拒絕且不建立待核准紀錄
    Given 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "<身分>" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 401
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 1 項驗收已勾選

    Examples:
      | 身分       |
      | 無憑證     |
      | 未登錄憑證 |

  Scenario: 僅待核准的憑證不能作為簽章金鑰申請的身分
    Given 為 "orders-client" 以待核准的新憑證提交登錄申請
    And 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "待核准憑證" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 401
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 1 項驗收已勾選

  Scenario: 已登錄 Client 替其他 Client 提出簽章金鑰申請被拒絕且對方的待核准清單不變
    Given 為 "orders-client" 已有待核准的新簽章金鑰登錄申請
    And 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "orders-partner-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 403
    And 失敗回應不含既有待核准簽章金鑰申請的識別
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 2 項驗收已勾選
    And 18 單第 6 項驗收已勾選

  Scenario Outline: 無法證明持有新金鑰私鑰的簽章金鑰申請被拒絕且不建立待核准紀錄
    Given 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "<證明>" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 400
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 3 項驗收已勾選

    Examples:
      | 證明     |
      | 缺漏     |
      | 他人私鑰 |

  Scenario: 已登錄 Client 為自己申請並證明持有新金鑰時建立帶已驗證身分的待核准紀錄
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 202
    And 管理員查詢簽章金鑰待核准申請的已驗證申請者為 "orders-client"
    And 18 單第 4 項驗收已勾選

  @isolated
  Scenario: 原始憑證已撤銷的 Client 不能提出簽章金鑰申請
    Given 管理員撤銷 "orders-client" 的原始憑證
    And 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 401
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 5 項驗收已勾選

  @isolated
  Scenario: 停用的 Client 不能提出簽章金鑰申請
    Given 管理員停用 "orders-client"
    And 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 401
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 5 項驗收已勾選

  @isolated
  Scenario: 原始憑證已退役的 Client 不能提出簽章金鑰申請
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And 管理員退役 "orders-client" 的原始憑證
    And 記錄 "orders-client" 的已受理登錄稽核基準
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 401
    And "orders-client" 的已受理登錄稽核數仍為基準值
    And 18 單第 5 項驗收已勾選

  Scenario: 未驗證者無法區分 Client 不存在與身分驗證失敗
    When 無憑證分別為 "no-such-client" 與 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 未驗證的簽章金鑰申請回應狀態與內容相同
    And 18 單第 6 項驗收已勾選

  Scenario: 憑證申請通過不代表簽章金鑰申請通過
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "缺漏" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 400
    And 18 單第 7 項驗收已勾選

  Scenario: 待核准的憑證申請不影響簽章金鑰申請的通過
    Given 為 "orders-client" 提交只含公開憑證的登錄申請
    When 以 "orders-client" 身分為 "orders-client" 提出持有證明為 "有效" 的新簽章金鑰登錄申請
    Then 簽章金鑰申請者驗證回應為 202
    And 18 單第 7 項驗收已勾選

  @record
  Scenario: 18 單的實作紀錄說明首次登錄範圍與持有證明為 lab 暫定值
    Then 18 單的實作紀錄包含 "首次登錄"
    And 18 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 18 單的實作紀錄包含 "SigningKeyApplicantVerification"
    And 18 單第 8 項驗收已勾選
