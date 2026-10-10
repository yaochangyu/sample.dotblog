Feature: 管理員退役與撤銷憑證與簽章金鑰
  依 14 單驗收項目：只有已驗證的管理員能退役或撤銷 Client 的憑證與簽章金鑰；
  退役沿用輪替重疊規則（必須已有可用的替代項目），撤銷沿用 60 秒上限；
  非管理員（包括該 Client 自己）不能退役或撤銷；管理操作只影響指定的 Client。
  退役與撤銷的端點與回應格式為 lab 暫定值，待使用者確認。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @isolated
  Scenario: 重疊期內新舊憑證皆可取得 Token，管理員退役舊憑證後舊憑證被拒絕且新憑證仍可用
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    Then "原始憑證" 要求 "orders-client" 的 Token 成功
    And "替代憑證" 要求 "orders-client" 的 Token 成功
    When 管理員退役 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 200
    And "原始憑證" 要求 "orders-client" 的 Token 被拒絕
    And "替代憑證" 要求 "orders-client" 的 Token 成功
    And 14 單第 1 項驗收已勾選

  @isolated
  Scenario: 重疊期內新舊簽章金鑰皆可通過驗證，管理員退役舊金鑰後舊金鑰簽署的請求被拒絕
    Given "orders-client" 已有經核准的替代簽章金鑰並進入重疊期
    And "orders-client" 以原始憑證取得 Token 並保持既有連線
    Then 以 "原始簽章金鑰" 送出建立訂單請求回應為 201
    And 以 "替代簽章金鑰" 送出建立訂單請求回應為 201
    When 管理員退役 "orders-client" 的原始簽章金鑰
    Then 退役撤銷管理操作回應為 200
    And 以 "原始簽章金鑰" 送出建立訂單請求回應為 401
    And 以 "替代簽章金鑰" 送出建立訂單請求回應為 201
    And 14 單第 1 項驗收已勾選

  @isolated
  Scenario: 沒有替代項目時退役被拒絕，信任名單與簽章金鑰狀態不變
    Given 記錄退役撤銷信任名單基準
    And "orders-client" 以原始憑證取得 Token 並保持既有連線
    When 管理員退役 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 409
    And 信任名單與退役撤銷基準相同
    And "原始憑證" 要求 "orders-client" 的 Token 成功
    When 管理員退役 "orders-client" 的原始簽章金鑰
    Then 退役撤銷管理操作回應為 409
    And 以 "原始簽章金鑰" 送出建立訂單請求回應為 201
    And 14 單第 1 項驗收已勾選

  @isolated
  Scenario: 撤銷憑證後既有連線上的請求在 60 秒內被阻擋
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And "orders-client" 以原始憑證取得 Token 並保持既有連線
    And 以 "原始簽章金鑰" 送出建立訂單請求回應為 201
    When 管理員撤銷 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 200
    And 既有連線的建立訂單請求在 60 秒內被阻擋
    And "原始憑證" 要求 "orders-client" 的 Token 被拒絕
    And "替代憑證" 要求 "orders-client" 的 Token 成功
    And 14 單第 2 項驗收已勾選

  @isolated
  Scenario: 撤銷唯一憑證後既有連線在 60 秒內被阻擋且不再核發 Token
    Given "orders-client" 以原始憑證取得 Token 並保持既有連線
    And 以 "原始簽章金鑰" 送出建立訂單請求回應為 201
    When 管理員撤銷 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 200
    And 既有連線的建立訂單請求在 60 秒內被阻擋
    And "原始憑證" 要求 "orders-client" 的 Token 被拒絕
    And 14 單第 2 項驗收已勾選

  @isolated
  Scenario: 撤銷簽章金鑰後既有連線上的請求在 60 秒內被阻擋，憑證不受影響
    Given "orders-client" 以原始憑證取得 Token 並保持既有連線
    And 以 "原始簽章金鑰" 送出建立訂單請求回應為 201
    When 管理員撤銷 "orders-client" 的原始簽章金鑰
    Then 退役撤銷管理操作回應為 200
    And 既有連線的建立訂單請求在 60 秒內被阻擋
    And "原始憑證" 要求 "orders-client" 的 Token 成功
    And 14 單第 2 項驗收已勾選

  Scenario Outline: 非管理員不能退役或撤銷 Client 的憑證，信任名單不變
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And 記錄退役撤銷信任名單基準
    When 以 "<身分>" 身分退役 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 401
    When 以 "<身分>" 身分撤銷 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 401
    And 信任名單與退役撤銷基準相同
    And "原始憑證" 要求 "orders-client" 的 Token 成功
    And 14 單第 3 項驗收已勾選

    Examples:
      | 身分         |
      | 無憑證       |
      | 未登錄憑證   |
      | Client 憑證  |

  Scenario Outline: 非管理員不能退役或撤銷 Client 的簽章金鑰，既有金鑰仍可通過驗證
    Given "orders-client" 已有經核准的替代簽章金鑰並進入重疊期
    And "orders-client" 以原始憑證取得 Token 並保持既有連線
    When 以 "<身分>" 身分退役 "orders-client" 的原始簽章金鑰
    Then 退役撤銷管理操作回應為 401
    When 以 "<身分>" 身分撤銷 "orders-client" 的原始簽章金鑰
    Then 退役撤銷管理操作回應為 401
    And 以 "原始簽章金鑰" 送出建立訂單請求回應為 201
    And 14 單第 3 項驗收已勾選

    Examples:
      | 身分         |
      | 無憑證       |
      | 未登錄憑證   |
      | Client 憑證  |

  @isolated
  Scenario: 退役與撤銷 orders-client 的項目不影響其他 Client 的憑證、金鑰與權限
    Given "orders-client" 已有經核准的替代憑證並進入重疊期
    And 記錄退役撤銷信任名單基準
    When 管理員退役 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 200
    When 管理員撤銷 "orders-client" 的原始簽章金鑰
    Then 退役撤銷管理操作回應為 200
    And 信任名單中除 "orders-client" 外與退役撤銷基準相同
    And "orders-partner-client" 的 Client 憑證仍可取得 Token
    And "billing-client" 的 Client 憑證仍可取得 Token
    And "orders-partner-client" 的 Client 以自身憑證與簽章金鑰建立訂單回應為 201
    And 14 單第 4 項驗收已勾選

  @isolated
  Scenario: 撤銷不存在或已撤銷的對象回應明確且狀態不變
    Given 記錄退役撤銷信任名單基準
    When 管理員撤銷 "orders-client" 的不存在憑證
    Then 退役撤銷管理操作回應為 404
    When 管理員撤銷不存在的 Client "not-registered-client" 的憑證
    Then 退役撤銷管理操作回應為 404
    When 管理員撤銷 "orders-client" 的不存在簽章金鑰
    Then 退役撤銷管理操作回應為 404
    When 管理員撤銷 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 200
    When 管理員撤銷 "orders-client" 的原始憑證
    Then 退役撤銷管理操作回應為 409
    And 信任名單中除 "orders-client" 外與退役撤銷基準相同
    And 14 單第 5 項驗收已勾選

  @record
  Scenario: 14 單的實作紀錄已標註退役與撤銷端點為 lab 暫定值
    Then 14 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 14 單第 6 項驗收已勾選
