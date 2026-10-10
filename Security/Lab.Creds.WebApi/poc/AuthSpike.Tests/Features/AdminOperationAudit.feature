Feature: 管理操作稽核
  依 15 單驗收項目：信任名單的每一次改變（登錄申請、核准、拒絕、退役、撤銷，以及停用）都留下管理操作稽核紀錄，
  可辨識操作者、時間、對象 Client、憑證或金鑰指紋與結果；紀錄不含私鑰與原始 Token；
  稽核寫入失敗時管理操作不生效並明確回報服務錯誤；未驗證的管理呼叫標示為「未驗證」；
  管理操作稽核與呼叫者安全稽核分開管理。
  稽核紀錄的路徑、欄位、保存期與存取規則皆為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @isolated
  Scenario: 憑證登錄申請與核准各留下可辨識操作者、對象、指紋與結果的紀錄
    Given 為 "orders-client" 提交待核准的憑證登錄申請
    When 稽核情境中以 "管理員憑證" 身分核准待核准的憑證申請
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核包含 "certificate.submit" 紀錄，對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "accepted"
    And 管理操作稽核包含 "certificate.approve" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "certificate.approve" 紀錄對象指紋為 "待核准憑證"
    And 15 單第 1 項驗收已勾選

  @isolated
  Scenario: 管理員拒絕憑證與簽章金鑰申請，各留下拒絕結果的紀錄
    Given 為 "orders-client" 提交待核准的憑證登錄申請
    When 稽核情境中以 "管理員憑證" 身分拒絕待核准的憑證申請
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核包含 "certificate.reject" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "certificate.reject" 紀錄對象指紋為 "待核准憑證"
    Given 為 "orders-client" 提交待核准的簽章金鑰登錄申請
    When 稽核情境中以 "管理員憑證" 身分拒絕待核准的簽章金鑰申請
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核包含 "signing_key.reject" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "signing_key.reject" 紀錄對象指紋為 "待核准簽章金鑰"
    And 15 單第 6 項驗收已勾選

  @isolated
  Scenario: 管理員退役憑證與簽章金鑰各留下紀錄
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And "orders-client" 已有經核准的替代簽章金鑰並進入重疊期
    When 稽核情境中以 "管理員憑證" 身分退役 "orders-client" 的原始憑證
    Then 稽核情境的管理操作回應為 200
    When 稽核情境中以 "管理員憑證" 身分退役 "orders-client" 的原始簽章金鑰
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核包含 "certificate.retire" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "certificate.retire" 紀錄對象指紋為 "原始憑證"
    And 管理操作稽核包含 "signing_key.retire" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "signing_key.retire" 紀錄對象指紋為 "原始簽章金鑰"

  @isolated
  Scenario: 管理員撤銷憑證與簽章金鑰各留下紀錄
    Given 稽核情境中 "orders-client" 以原始憑證取得 Token
    When 稽核情境中以 "管理員憑證" 身分撤銷 "orders-client" 的原始憑證
    Then 稽核情境的管理操作回應為 200
    When 稽核情境中以 "管理員憑證" 身分撤銷 "orders-client" 的原始簽章金鑰
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核包含 "certificate.revoke" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "certificate.revoke" 紀錄對象指紋為 "原始憑證"
    And 管理操作稽核包含 "signing_key.revoke" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"
    And 管理操作稽核的 "signing_key.revoke" 紀錄對象指紋為 "原始簽章金鑰"

  @isolated
  Scenario: 管理員停用 Client 留下紀錄
    When 稽核情境中以 "管理員憑證" 身分停用 "orders-client"
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核包含 "client.disable" 紀錄，對象 Client 為 "orders-client"，操作者為 "已驗證管理員"，結果為 "accepted"

  @isolated
  Scenario: 管理操作稽核紀錄不含私鑰或原始 Token
    Given 稽核情境中 "orders-client" 以原始憑證取得 Token
    And 為 "orders-client" 提交待核准的簽章金鑰登錄申請
    When 稽核情境中以 "管理員憑證" 身分核准待核准的簽章金鑰申請
    Then 稽核情境的管理操作回應為 200
    And 管理操作稽核不含私鑰、原始 Token 或憑證私有內容
    And 15 單第 2 項驗收已勾選

  @isolated
  Scenario: 管理操作稽核寫入失敗時，核准不生效並明確回報服務錯誤
    Given 為 "orders-client" 提交待核准的憑證登錄申請
    And 記錄稽核情境的信任名單基準
    And 管理操作稽核暫時無法寫入
    When 稽核情境中以 "管理員憑證" 身分核准待核准的憑證申請
    Then 稽核情境的管理操作回應為 503
    And 稽核情境的待核准申請狀態為 "pending"
    And 信任名單與稽核情境基準相同
    And 稽核情境的待核准憑證無法取得 Token
    And 15 單第 3 項驗收已勾選

  @isolated
  Scenario: 管理操作稽核寫入失敗時，退役與撤銷不生效
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And 記錄稽核情境的信任名單基準
    And 管理操作稽核暫時無法寫入
    When 稽核情境中以 "管理員憑證" 身分退役 "orders-client" 的原始憑證
    Then 稽核情境的管理操作回應為 503
    And 信任名單與稽核情境基準相同
    And 稽核情境中 "orders-client" 的原始憑證仍可取得 Token
    And 15 單第 3 項驗收已勾選

  Scenario Outline: 未經管理員驗證的呼叫被拒絕，且稽核紀錄標示為未驗證
    Given 為 "orders-client" 提交待核准的憑證登錄申請
    When 稽核情境中以 "<身分>" 身分核准待核准的憑證申請
    Then 稽核情境的管理操作回應為 401
    And 管理操作稽核包含 "certificate.approve" 紀錄，對象 Client 為 "orders-client"，操作者為 "未驗證"，結果為 "rejected"
    And 稽核情境的待核准申請狀態為 "pending"
    And 15 單第 4 項驗收已勾選

    Examples:
      | 身分         |
      | Client 憑證  |
      | 無憑證       |
      | 未登錄憑證   |

  @isolated
  Scenario: 管理操作稽核與呼叫者安全稽核分開管理
    Given 為 "orders-client" 提交待核准的憑證登錄申請
    And 記錄呼叫者安全稽核紀錄數
    When 稽核情境中以 "管理員憑證" 身分核准待核准的憑證申請
    Then 稽核情境的管理操作回應為 200
    And 呼叫者安全稽核紀錄數未改變
    And 管理操作稽核與呼叫者安全稽核為不同的紀錄集合

  @record
  Scenario: 管理操作稽核的保存期與存取規則標示為 lab 暫定待使用者確認
    Then 15 單的實作紀錄包含 "待使用者確認"
    And 15 單的實作紀錄包含 "保存期"
    And 15 單第 5 項驗收已勾選
