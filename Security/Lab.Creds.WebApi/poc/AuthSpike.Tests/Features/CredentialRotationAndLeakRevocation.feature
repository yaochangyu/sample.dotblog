Feature: 不中斷的正常輪替與洩漏撤銷
  依 08 單驗收項目，確認 mTLS 憑證與請求簽章金鑰可分開輪替；重疊期內新舊配置都能合法呼叫，
  退役後舊配置不能通過；疑似洩漏時不等待重疊期結束，於 60 秒內阻擋。
  每 Client 每環境獨立配置私鑰。本 lab 不實作 Gateway，呼叫端直接呼叫業務 API。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 08 單的實作紀錄可讀取

  @record
  Scenario: 輪替重疊期與操作程序已記錄於實作紀錄
    Then 08 單第 1 項驗收已勾選
    And 08 單的實作紀錄包含 "重疊期"
    And 08 單的實作紀錄包含 "操作程序"
    And 08 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 08 單的實作紀錄包含 "mTLS 憑證與請求簽章金鑰分開管理"

  Scenario: 每 Client 每環境獨立配置私鑰
    Given 另一個環境 "production" 的授權伺服器與建立訂單 API 已啟動
    Then "orders-client" 在 "lab" 與 "production" 的 mTLS 憑證指紋互不相同
    And "orders-client" 在 "lab" 與 "production" 的簽章金鑰識別與公開金鑰互不相同
    And "orders-client" 與 "orders-partner-client" 的簽章金鑰識別互不相同
    When 呼叫端以 "production" 環境的 mTLS 憑證向該環境授權伺服器要求 Token
    And 呼叫端以 "lab" 環境的簽章金鑰送出建立訂單請求
    Then 輪替情境的建立訂單回應為 401
    And 08 單第 2 項驗收已勾選

  @isolated
  Scenario: 正常輪替依登錄新 mTLS 憑證、切換呼叫端、再退役舊憑證的順序進行
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "憑證輪替前訂單"
    When 管理者嘗試在未登錄替代時退役 "orders-client" 的既有 "mTLS 憑證"
    Then 退役被拒絕且原因為 "必須先登錄替代的憑證"
    When 管理者為 "orders-client" 產生新的 "mTLS 憑證" 但尚未登錄
    And 呼叫端以尚未登錄的新 mTLS 憑證向授權伺服器要求 Token
    Then 輪替情境的 Token 要求未核發
    When 管理者登錄新產生的 "mTLS 憑證"
    And 呼叫端切換為新的 "mTLS 憑證"
    And 呼叫端以目前 mTLS 憑證向授權伺服器要求 Token
    Then 輪替情境的 Token 要求已核發
    When 管理者退役 "orders-client" 的既有 "mTLS 憑證"
    Then 既有 "mTLS 憑證" 已列為退役
    And 08 單第 3 項驗收已勾選

  @isolated
  Scenario: 正常輪替依登錄新簽章金鑰、切換呼叫端、再退役舊金鑰的順序進行
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "金鑰輪替前訂單"
    When 管理者嘗試在未登錄替代時退役 "orders-client" 的既有 "簽章金鑰"
    Then 退役被拒絕且原因為 "必須先登錄替代的金鑰"
    When 管理者為 "orders-client" 產生新的 "簽章金鑰" 但尚未登錄
    And 呼叫端以尚未登錄的新簽章金鑰送出查詢訂單 "金鑰輪替前訂單"
    Then 輪替情境的查詢回應為 401
    When 管理者登錄新產生的 "簽章金鑰"
    And 呼叫端切換為新的 "簽章金鑰"
    And 呼叫端以目前配置查詢訂單 "金鑰輪替前訂單"
    Then 輪替情境的查詢回應為 200
    When 管理者退役 "orders-client" 的既有 "簽章金鑰"
    Then 既有 "簽章金鑰" 已列為退役
    And 08 單第 3 項驗收已勾選

  @isolated
  Scenario: 重疊期內新舊配置都能合法呼叫，退役後舊配置不能通過
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "舊配置訂單"
    When 管理者為 "orders-client" 產生並登錄新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端切換為新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端以目前 mTLS 憑證向授權伺服器要求 Token
    And 呼叫端以目前配置建立訂單 "新配置訂單"
    Then 輪替情境的建立訂單回應為 201
    When 呼叫端以既有配置查詢訂單 "舊配置訂單"
    Then 輪替情境的查詢回應為 200
    When 管理者退役 "orders-client" 的既有 "mTLS 憑證與簽章金鑰"
    Then 既有 "mTLS 憑證與簽章金鑰" 已列為退役
    When 呼叫端以既有配置查詢訂單 "舊配置訂單"
    Then 輪替情境的查詢回應為 401
    When 呼叫端以既有配置向授權伺服器要求 Token
    Then 輪替情境的 Token 要求被拒絕
    When 呼叫端以目前配置查詢訂單 "新配置訂單"
    Then 輪替情境的查詢回應為 200
    And 08 單第 4 項驗收已勾選

  @isolated
  Scenario: 新憑證取得綁定自己的 Token，舊 Token 搭配新憑證被拒絕
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "舊 Token 訂單"
    When 管理者為 "orders-client" 產生並登錄新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端切換為新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端以既有 Token 搭配目前配置查詢訂單 "舊 Token 訂單"
    Then 輪替情境的查詢回應為 401
    When 呼叫端以目前 mTLS 憑證向授權伺服器要求 Token
    Then 輪替情境的 Token 要求已核發
    When 呼叫端以目前配置建立訂單 "新 Token 訂單"
    Then 輪替情境的建立訂單回應為 201
    And 08 單第 5 項驗收已勾選

  @isolated
  Scenario: 只輪替 mTLS 憑證時簽章金鑰狀態不被連動
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "分開輪替訂單"
    When 管理者為 "orders-client" 產生並登錄新的 "mTLS 憑證"
    And 呼叫端切換為新的 "mTLS 憑證"
    And 呼叫端以目前 mTLS 憑證向授權伺服器要求 Token
    And 呼叫端以目前配置建立訂單 "新 mTLS 訂單"
    Then 輪替情境的建立訂單回應為 201
    And 既有 "簽章金鑰" 仍未列為退役
    When 管理者退役 "orders-client" 的既有 "mTLS 憑證"
    Then 既有 "mTLS 憑證" 已列為退役
    When 呼叫端以目前配置查詢訂單 "分開輪替訂單"
    Then 輪替情境的查詢回應為 200
    When 呼叫端以既有配置查詢訂單 "分開輪替訂單"
    Then 輪替情境的查詢回應為 401
    And 08 單第 6 項驗收已勾選

  @isolated
  Scenario: 只輪替簽章金鑰時 mTLS 憑證狀態不被連動
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "分開簽章訂單"
    When 管理者為 "orders-client" 產生並登錄新的 "簽章金鑰"
    And 呼叫端切換為新的 "簽章金鑰"
    And 呼叫端以目前配置建立訂單 "新簽章金鑰訂單"
    Then 輪替情境的建立訂單回應為 201
    And 既有 "mTLS 憑證" 仍未列為退役
    When 管理者退役 "orders-client" 的既有 "簽章金鑰"
    Then 既有 "簽章金鑰" 已列為退役
    When 呼叫端以既有配置查詢訂單 "分開簽章訂單"
    Then 輪替情境的查詢回應為 401
    When 呼叫端以目前配置查詢訂單 "分開簽章訂單"
    Then 輪替情境的查詢回應為 200
    When 呼叫端以既有配置向授權伺服器要求 Token
    Then 輪替情境的 Token 要求已核發
    And 08 單第 6 項驗收已勾選

  @isolated
  Scenario Outline: 重疊期內疑似洩漏時直接撤銷<對象>，既有配置於 60 秒內被阻擋
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "洩漏前訂單"
    When 管理者為 "orders-client" 產生並登錄新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端切換為新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端以目前 mTLS 憑證向授權伺服器要求 Token
    Then 既有配置尚未退役
    When 疑似洩漏時撤銷 "<對象>"
    Then 既有配置的查詢於 60 秒內回應 401
    And 目前配置的查詢仍回應 200
    And 08 單第 7 項驗收已勾選

    Examples:
      | 對象         |
      | 既有 mTLS 憑證 |
      | 既有簽章金鑰   |
      | 既有 Token     |

  @isolated
  Scenario: 重疊期內停用 Client 時新舊配置都於 60 秒內被阻擋
    Given 輪替情境中簽章呼叫端 "orders-client" 以既有配置取得 Token 並成功建立訂單 "停用前訂單"
    When 管理者為 "orders-client" 產生並登錄新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端切換為新的 "mTLS 憑證與簽章金鑰"
    And 呼叫端以目前 mTLS 憑證向授權伺服器要求 Token
    And 疑似洩漏時撤銷 "Client"
    Then 既有配置的查詢於 60 秒內回應 401
    And 目前配置的查詢於 60 秒內回應 401
    And 08 單第 7 項驗收已勾選

  @record
  Scenario: 輪替與洩漏處理說明可重現且文件不含真實憑證或私鑰
    Then 08 單的實作紀錄包含 "輪替步驟"
    And 08 單的實作紀錄包含 "洩漏處理步驟"
    And 08 單的實作紀錄包含 "dotnet test"
    And 08 單的實作紀錄不包含真實私鑰或憑證內容
    And 08 單第 8 項驗收已勾選
