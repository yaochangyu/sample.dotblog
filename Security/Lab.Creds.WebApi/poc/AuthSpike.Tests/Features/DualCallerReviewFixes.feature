Feature: 雙呼叫者審查意見修正（Standards／Spec 審查）
  審查指出的行為與文件不一致。行為變更先以本檔情境確認紅燈，再修改產品程式；文件項目以文字檢查情境確認。

  @dual-caller-review @doc
  Scenario: 審查修正 OpenAPI 為建立訂單宣告 X-Request-Nonce 標頭與使用者呼叫者的 401 說明
    Then 審查修正的 OpenAPI 包含 "- name: X-Request-Nonce"
    And 審查修正的 OpenAPI 包含 "使用者呼叫者缺少或重放 X-Request-Nonce 時回應 401"

  @dual-caller-review @doc
  Scenario: 審查修正雙呼叫者規格檔頭不再標為尚未實作或 ready-for-agent
    Then 雙呼叫者文件 ".scratch/dual-caller-authentication/spec.md" 不包含 "尚未實作"
    And 雙呼叫者文件 ".scratch/dual-caller-authentication/spec.md" 不包含 "Status: ready-for-agent"

  @dual-caller-review @doc
  Scenario: 審查修正 06 與 07 單的狀態與勾選一致，第 6 項未照字面達成者不得勾選
    Then 雙呼叫者文件 ".scratch/dual-caller-authentication/issues/06-user-token-revocation-and-fail-closed.md" 不包含 "**Status:** resolved"
    And 雙呼叫者文件 ".scratch/dual-caller-authentication/issues/06-user-token-revocation-and-fail-closed.md" 不包含 "- [x] 每個驗收項目至少對應一個 BDD Scenario"
    And 雙呼叫者文件 ".scratch/dual-caller-authentication/issues/07-caller-type-audit.md" 不包含 "**Status:** resolved"
    And 雙呼叫者文件 ".scratch/dual-caller-authentication/issues/07-caller-type-audit.md" 不包含 "- [x] 每個驗收項目至少對應一個 BDD Scenario"

  @dual-caller-review @doc
  Scenario: 審查判斷的 lab 刻意保留項目已記錄於 ADR 0001
    Then 審查修正的 ADR 0001 包含 "服務專屬操作"
    And 審查修正的 ADR 0001 包含 "角色模型"
    And 審查修正的 ADR 0001 包含 "撤銷延遲"

  @dual-caller-review @service-path
  Scenario: 服務路徑拒絕宣稱 caller_type=user 的 Token，不以使用者身分處理
    Given 查證服務以假的授權伺服器取代，introspection 行為為 "active-service-with-user-caller-type"
    When 呼叫端持任意 Token 查詢訂單
    Then 查證故障情境的回應為 401

  @dual-caller-review @service-path
  Scenario: 服務路徑拒絕 client_id 以 user: 開頭的 Token，避免與使用者業務範圍鍵碰撞
    Given 查證服務以假的授權伺服器取代，introspection 行為為 "active-service-with-user-prefixed-client"
    When 呼叫端持任意 Token 查詢訂單
    Then 查證故障情境的回應為 401
    And 查證故障情境最近一次稽核原因為 "caller_rejected"

  @dual-caller-review @user-path
  Scenario: 使用者路徑內省回應缺少必要欄位時視為 Token 無效（401），不誤報為查證服務故障
    Given 查證服務以假的授權伺服器取代，introspection 行為為 "active-without-caller-type"
    When 使用者呼叫端不附憑證持任意 Token 查詢訂單
    Then 查證故障情境的回應為 401

  @dual-caller-review @user-path
  Scenario: 使用者路徑內省無回應時於查證逾時上限內回應 503，不無限等待
    Given 查證服務以假的授權伺服器取代，introspection 行為為 "hang"
    When 使用者呼叫端不附憑證持任意 Token 查詢訂單
    Then 查證故障情境的回應為 503
    And 查證故障情境於 30 秒內回應
