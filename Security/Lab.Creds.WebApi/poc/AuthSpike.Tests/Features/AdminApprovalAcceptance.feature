Feature: 管理員核准的整體驗收
  依 16 單驗收項目，以管理員核准為主題，端到端串接 AC-15 至 AC-23：
  Client 提交新憑證的登錄申請、管理員核准後該憑證才能取得 Token 並呼叫建立訂單、
  管理員退役原始憑證與撤銷新憑證後各自被拒絕、管理操作稽核可查詢且不含私鑰與原始 Token。
  另確認 AC-15 至 AC-23 各有對應情境、GLOSSARY 與 ADR 已補記、待使用者確認項目已列出。
  本票不新增功能，不實作 Gateway；管理員憑證、申請欄位與稽核保存期皆為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 16 單的實作紀錄可讀取

  @isolated
  Scenario: 申請、核准、取得 Token 呼叫、退役、撤銷與稽核查詢串成一條端到端流程
    Given 為 "orders-client" 以新憑證提交待核准的登錄申請
    Then 端到端新憑證取得 Token 被拒絕
    And 端到端信任名單不包含新憑證指紋
    When 端到端管理員核准新憑證的登錄申請
    Then 端到端管理操作回應為 200
    And 端到端信任名單中 "orders-client" 包含新憑證指紋
    Then 端到端新憑證取得 Token 並以該 Token 建立訂單，回應為 201
    When 端到端管理員退役 "orders-client" 的原始憑證
    Then 端到端管理操作回應為 200
    And 端到端原始憑證取得 Token 被拒絕
    And 端到端新憑證取得 Token 並以該 Token 建立訂單，回應為 201
    When 端到端管理員撤銷 "orders-client" 的新憑證
    Then 端到端管理操作回應為 200
    And 端到端新憑證取得 Token 被拒絕
    And 管理操作稽核包含 "certificate.submit" 紀錄，對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "accepted"
    And 管理操作稽核包含 "certificate.approve" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核包含 "certificate.retire" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核包含 "certificate.revoke" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 端到端稽核紀錄不含私鑰標記與本次取得的 access_token

  @record
  Scenario: 16 單的 AC 對應表列出 AC-15 至 AC-23 且每個情境名稱都存在於 feature 檔
    Then 16 單的 AC 對應表列出 9 列
    And 16 單的 AC 對應表中每個情境名稱都存在於 feature 檔

  @record
  Scenario: GLOSSARY 補上管理員與登錄申請並與既有詞彙區分
    Then GLOSSARY 包含詞條 "**管理員**"
    And GLOSSARY 包含詞條 "**登錄申請**"
    And GLOSSARY 的管理員詞條區分於 "Client 身分" 與 "已驗證呼叫者"
    And 16 單第 3 項驗收已勾選

  @record
  Scenario: ADR 0001 補記管理介面的 lab 例外與限制
    Then ADR 0001 包含 "**管理介面**"
    And ADR 0001 包含 "單程序"
    And ADR 0001 包含 "程序內"
    And 16 單第 4 項驗收已勾選

  @record
  Scenario Outline: 16 單列出待使用者確認的項目 <項目>
    Then 16 單的實作紀錄包含 "<項目>"
    And 16 單第 5 項驗收已勾選

    Examples:
      | 項目           |
      | 申請傳遞管道   |
      | 多人覆核       |
      | 管理員憑證頒發 |
      | 管理介面格式   |
      | 稽核保存期     |

  @record
  Scenario: 16 單的實作紀錄說明可重現驗證方式並記錄實際結果
    Then 16 單的實作紀錄包含 "dotnet test"
    And 16 單的實作紀錄包含 "AdminApprovalAcceptance"
    And 16 單的實作紀錄包含 "Gateway"
    And 16 單第 1 項驗收已勾選
    And 16 單第 2 項驗收已勾選
    And 16 單第 6 項驗收已勾選
