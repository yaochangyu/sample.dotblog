Feature: 稽核區分呼叫者類型（07 單）
  依 07 單驗收項目：追查紀錄記錄已驗證呼叫者的類型與身分，能分辨服務呼叫與使用者呼叫（AC-D13）；
  使用者請求不被記成服務，服務請求不被記成使用者；驗證失敗的請求，其宣稱的呼叫者類型與身分只標為未驗證；
  紀錄不含原始 Token、私鑰與完整敏感 Body；稽核寫入失敗時沿用 fail closed，不放行業務操作並回報 audit_unavailable。
  新增的呼叫者類型與身分欄位為 lab 暫定值，待使用者確認。本 lab 以測試使用者帳號代替真實登入流程。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @caller-type-audit
  Scenario: 服務呼叫的成功紀錄標示已驗證呼叫者類型為服務，身分為服務的 Client
    When 類型稽核票服務 "orders-client" 以已簽章請求建立訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "服務成功"
    Then 類型稽核票最近一次請求回應 201
    And 類型稽核票紀錄 "服務成功" 為:
      | 結果               | accepted         |
      | 原因               | signature_verified |
      | 已驗證呼叫者類型   | service          |
      | 已驗證身分         | orders-client    |
      | 已驗證 Client      | orders-client    |
      | 未驗證呼叫者類型   | 空               |
      | 未驗證身分         | 空               |

  @caller-type-audit
  Scenario: 使用者呼叫的成功紀錄標示已驗證呼叫者類型為使用者，身分為使用者而非 Client
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    When 類型稽核票使用者以使用者 Token 建立訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "使用者成功"
    Then 類型稽核票最近一次請求回應 201
    And 類型稽核票紀錄 "使用者成功" 為:
      | 結果               | accepted           |
      | 原因               | user_token_verified |
      | 已驗證呼叫者類型   | user               |
      | 已驗證身分         | test-user-1        |
      | 已驗證 Client      | 空                 |
      | 最終業務結果       | 201                |

  @caller-type-audit
  Scenario: 使用者讀取自己的訂單也標示為使用者呼叫
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    When 類型稽核票使用者以使用者 Token 建立訂單
    And 類型稽核票使用者以使用者 Token 讀取剛建立的訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "使用者讀取"
    Then 類型稽核票最近一次請求回應 200
    And 類型稽核票紀錄 "使用者讀取" 為:
      | 結果               | accepted            |
      | 已驗證呼叫者類型   | user                |
      | 已驗證身分         | test-user-1         |
      | 已驗證 Client      | 空                  |

  @caller-type-audit
  Scenario: 同一時間的使用者請求與服務請求各自記為正確的呼叫者類型，互不混淆
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    When 類型稽核票服務 "orders-client" 以已簽章請求建立訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "服務請求"
    And 類型稽核票使用者以使用者 Token 建立訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "使用者請求"
    Then 類型稽核票紀錄 "服務請求" 為:
      | 已驗證呼叫者類型   | service            |
      | 已驗證身分         | orders-client      |
      | 已驗證 Client      | orders-client      |
    And 類型稽核票紀錄 "使用者請求" 為:
      | 已驗證呼叫者類型   | user               |
      | 已驗證身分         | test-user-1        |
      | 已驗證 Client      | 空                 |

  @caller-type-audit
  Scenario: 服務呼叫限定使用者的操作被拒絕，紀錄把服務宣稱的身分標為未驗證且不記為使用者
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    When 類型稽核票服務 "orders-client" 以已簽章請求查詢使用者專屬訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "服務越權"
    Then 類型稽核票最近一次請求回應 403
    And 類型稽核票紀錄 "服務越權" 為:
      | 結果               | denied             |
      | 原因               | status_403         |
      | 已驗證呼叫者類型   | 空                 |
      | 已驗證 Client      | 空                 |
      | 未驗證呼叫者類型   | service            |
      | 未驗證身分         | orders-client      |

  @caller-type-audit @isolated
  Scenario: 使用者被停用時的拒絕紀錄把宣稱的使用者標為未驗證且不記為已驗證
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    And 類型稽核票測試使用者 "test-user-1" 被停用
    When 類型稽核票使用者以使用者 Token 建立訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "停用拒絕"
    Then 類型稽核票最近一次請求回應 401
    And 類型稽核票紀錄 "停用拒絕" 為:
      | 結果               | rejected           |
      | 原因               | caller_rejected    |
      | 已驗證呼叫者類型   | 空                 |
      | 已驗證身分         | 空                 |
      | 已驗證 Client      | 空                 |
      | 未驗證呼叫者類型   | user               |
      | 未驗證身分         | test-user-1        |
      | 未驗證 Client      | 空                 |
    And 類型稽核票業務 API 建立訂單總數為 0

  @caller-type-audit
  Scenario: 重送相同使用者 nonce 的拒絕紀錄把宣稱的使用者標為未驗證
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    When 類型稽核票使用者以使用者 Token 建立訂單
    And 類型稽核票使用者以相同 X-Request-Nonce 重送建立訂單
    And 類型稽核票把最近一次請求的關聯識別記為 "使用者重送"
    Then 類型稽核票最近一次請求回應 401
    And 類型稽核票紀錄 "使用者重送" 為:
      | 結果               | rejected           |
      | 原因               | user_request_replayed |
      | 已驗證呼叫者類型   | 空                 |
      | 已驗證身分         | 空                 |
      | 未驗證呼叫者類型   | user               |
      | 未驗證身分         | test-user-1        |

  @caller-type-audit
  Scenario: 稽核紀錄不含使用者 Token、服務 Token、簽章私鑰與完整敏感 Body
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    When 類型稽核票使用者以使用者 Token 建立訂單，品項為 "機密品項-類型稽核"
    And 類型稽核票服務 "orders-client" 以已簽章請求建立訂單
    Then 類型稽核票最近一次請求回應 201
    And 類型稽核票稽核紀錄全部不含使用者 Token 與服務 Token
    And 類型稽核票稽核紀錄全部不含 "機密品項-類型稽核"
    And 類型稽核票稽核紀錄全部不含 "orders-client" 的簽章私鑰

  @caller-type-audit @isolated
  Scenario: 使用者呼叫的稽核寫入失敗時沿用 fail closed，不放行業務操作並回報 audit_unavailable
    Given 類型稽核票使用者 "test-user-1" 取得使用者 Token
    And 類型稽核票稽核紀錄暫時無法寫入
    When 類型稽核票使用者以使用者 Token 建立訂單
    Then 類型稽核票最近一次請求回應 503 且錯誤代碼為 "audit_unavailable"
    And 類型稽核票業務 API 建立訂單總數為 0

  @record
  Scenario: 07 單的第 1 至 5 項驗收已勾選且實作紀錄標註 lab 暫定值
    Then 類型稽核票第 1 項驗收已勾選
    And 類型稽核票第 2 項驗收已勾選
    And 類型稽核票第 3 項驗收已勾選
    And 類型稽核票第 4 項驗收已勾選
    And 類型稽核票第 5 項驗收已勾選
    And 類型稽核票的實作紀錄包含 "lab 暫定、待使用者確認"
