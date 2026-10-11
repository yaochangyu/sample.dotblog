Feature: 申請者驗證的整體驗收
  依 21 單驗收項目，以申請者驗證為主題，端到端串接 AC-24 至 AC-30：
  已登錄 Client 以現有憑證為自己申請並證明持有新私鑰、管理員核准、新憑證取得 Token 並呼叫建立訂單；
  同時攻擊者的匿名申請、替他人申請、無持有證明、超過待核准上限都被明確拒絕，且不建立待核准紀錄。
  另確認 AC-24 至 AC-30 各有對應且實際通過的情境、GLOSSARY 與 ADR 已補記、待使用者確認項目已列出。
  本票不新增功能，不實作 Gateway；持有證明、待核准上限與逾期處理皆為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 21 單的實作紀錄可讀取

  @isolated
  Scenario: 申請、證明持有、核准、取得 Token 呼叫與攻擊者被擋下串成一條端到端流程
    When 驗收端到端 "orders-client" 以自己的身分提出持有證明有效的新憑證申請
    Then 驗收端到端申請回應為 202
    When 驗收端到端匿名提出持有證明有效的新憑證申請
    Then 驗收端到端申請回應為 401
    When 驗收端到端 "orders-partner-client" 替 "orders-client" 提出持有證明有效的新憑證申請
    Then 驗收端到端申請回應為 403
    When 驗收端到端 "orders-client" 提出缺少持有證明的新憑證申請
    Then 驗收端到端申請回應為 400
    And 驗收端到端 "orders-client" 已受理的申請數為 1
    When 驗收端到端 "orders-client" 再提出 4 筆持有證明有效的新憑證申請
    And 驗收端到端 "orders-client" 提出第 6 筆持有證明有效的新憑證申請
    Then 驗收端到端申請回應為 429
    And 驗收端到端 "orders-client" 已受理的申請數為 5
    And 驗收端到端新憑證取得 Token 被拒絕
    When 驗收端到端管理員核准第一筆申請
    Then 驗收端到端管理操作回應為 200
    And 驗收端到端新憑證取得 Token 並建立訂單，回應為 201
    And 驗收端到端 "orders-client" 的未驗證拒絕申請紀錄至少 4 筆
    And 驗收端到端稽核紀錄不含私鑰標記與取得的 access_token

  @record
  Scenario: 21 單的 AC 對應表列出 AC-24 至 AC-30 共 7 列且每個情境名稱都存在於 feature 檔
    Then 21 單的 AC 對應表列出 7 列
    And 21 單的 AC 對應表中每個情境名稱都存在於 feature 檔

  @record
  Scenario: GLOSSARY 補上申請者與持有證明並與既有詞彙區分
    Then 21 單的 GLOSSARY 包含詞條 "**申請者**"
    And 21 單的 GLOSSARY 包含詞條 "**持有證明**"
    And 21 單的 GLOSSARY 詞條 "**申請者**" 區分於 "Client 身分" 與 "已驗證呼叫者"
    And 21 單第 3 項驗收已勾選

  @record
  Scenario: ADR 0001 移除申請端點未驗證的缺口並補記申請者驗證的限制
    Then 21 單的 ADR 0001 不包含 "登錄申請端點未驗證呼叫者"
    And 21 單的 ADR 0001 包含 "全新 Client"
    And 21 單的 ADR 0001 包含 "單程序"
    And 21 單的 ADR 0001 包含 "申請者驗證"
    And 21 單第 4 項驗收已勾選

  @record
  Scenario Outline: 21 單列出待使用者確認的項目 <項目>
    Then 21 單的實作紀錄包含 "<項目>"
    And 21 單第 5 項驗收已勾選

    Examples:
      | 項目             |
      | 待核准數上限的數值 |
      | 持有證明的表示方式 |
      | 簽章涵蓋範圍       |
      | 逾期處理          |

  @record
  Scenario: 21 單的實作紀錄說明可重現驗證方式、不實作 Gateway 並記錄實際結果
    Then 21 單的實作紀錄包含 "dotnet test"
    And 21 單的實作紀錄包含 "ApplicantVerificationAcceptance"
    And 21 單的實作紀錄包含 "Gateway"
    And 21 單第 6 項驗收已勾選
