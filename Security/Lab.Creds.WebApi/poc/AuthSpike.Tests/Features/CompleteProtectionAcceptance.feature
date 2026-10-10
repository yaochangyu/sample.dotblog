Feature: 完整接入流程與規格驗收
  依 10 單驗收項目，在同一整合測試邊界（呼叫端直連業務 API，本 lab 不經 Gateway）串接正常接入與錯誤流程，
  確認母規格 AC-01 至 AC-14 皆有對應情境、所有業務入口都經過請求保護、診斷輸出不洩漏敏感資料，
  並確認實作紀錄可供內外部服務重現。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動

  Scenario: 10 單的驗收項目與 BDD 情境對應表完整
    Given 10 單的實作紀錄可讀取
    Then 10 單的 "驗收項目" 對應表列出 9 列
    And 10 單的 "驗收項目" 對應表中每個情境名稱都存在於 feature 檔

  Scenario: 母規格 AC-01 至 AC-14 列出對應情境與可重現證據
    Given 10 單的實作紀錄可讀取
    Then 10 單的 "AC" 對應表列出 14 列
    And 10 單的 "AC" 對應表中每個情境名稱都存在於 feature 檔

  Scenario: 正常接入流程串接 mTLS 取得 Token、簽章、nonce、重試、資料範圍與業務去重
    Given 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    When 接入呼叫端以 "orders-client" 簽章送出建立訂單 "flow-ref-1"，Idempotency-Key 為 "flow-key-1"
    Then 接入回應為 201
    When 接入呼叫端以新 nonce 與相同 Idempotency-Key 重送上一筆訂單
    Then 接入回應為 201
    And 接入回應標示為重播
    And 業務訂單參考 "flow-ref-1" 的訂單數為 1
    When 接入呼叫端以 "orders-client" 簽章查詢上一筆訂單
    Then 接入回應為 200
    And 接入回應的 clientId 為 "orders-client"
    Given 接入呼叫端 "orders-partner-client" 以 mTLS 取得 Token
    When 接入呼叫端以 "orders-partner-client" 簽章查詢上一筆訂單
    Then 接入回應為 404
    When 接入呼叫端重送同一份已簽章的建立訂單請求
    Then 接入回應為 401
    And 業務訂單參考 "flow-ref-1" 的訂單數為 1

  Scenario Outline: 業務入口 <入口> 未簽章的請求被拒絕且不產生副作用
    Given 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    And 接入呼叫端以 "orders-client" 簽章送出建立訂單 "unsigned-ref-1"，Idempotency-Key 為 "unsigned-key-1"
    When 接入呼叫端以 Token 未簽章送出 "<入口>"
    Then 接入回應為 401
    And 上一筆訂單的狀態為 "active"

    Examples:
      | 入口     |
      | 建立訂單 |
      | 查詢訂單 |
      | 取消訂單 |

  Scenario: 簽章與 Token 有效的取消訂單成功，訂單狀態變為 cancelled
    Given 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    And 接入呼叫端以 "orders-client" 簽章送出建立訂單 "cancel-ref-1"，Idempotency-Key 為 "cancel-key-1"
    When 接入呼叫端以 "orders-client" 簽章取消上一筆訂單
    Then 接入回應為 200
    And 接入回應的狀態為 "cancelled"

  Scenario: 取消訂單天生冪等，不附 Idempotency-Key 的已簽章取消請求也成功，重複取消結果相同
    Given 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    And 接入呼叫端以 "orders-client" 簽章送出建立訂單 "cancel-ref-3"，Idempotency-Key 為 "cancel-key-3"
    When 接入呼叫端以 "orders-client" 簽章取消上一筆訂單，不附 Idempotency-Key
    Then 接入回應為 200
    And 接入回應的狀態為 "cancelled"
    When 接入呼叫端以 "orders-client" 簽章取消上一筆訂單，不附 Idempotency-Key
    Then 接入回應為 200
    And 接入回應的狀態為 "cancelled"

  Scenario: 已簽署的取消訂單改動目標被拒絕且訂單保持原狀
    Given 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    And 接入呼叫端以 "orders-client" 簽章送出建立訂單 "cancel-ref-2"，Idempotency-Key 為 "cancel-key-2"
    When 接入呼叫端以 "orders-client" 簽章取消訂單，但目標改為其他訂單編號
    Then 接入回應為 401
    And 上一筆訂單的狀態為 "active"

  Scenario Outline: 業務入口 <入口> 只附 API Key 或缺少 Token 時被拒絕
    Given 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    And 接入呼叫端以 "orders-client" 簽章送出建立訂單 "apikey-ref-1"，Idempotency-Key 為 "apikey-key-1"
    When 接入呼叫端不附 Token，只以 X-Api-Key 標頭送出 "<入口>"
    Then 接入回應為 401
    When 接入呼叫端不附任何授權標頭送出 "<入口>"
    Then 接入回應為 401
    And 上一筆訂單的狀態為 "active"

    Examples:
      | 入口     |
      | 查詢訂單 |
      | 取消訂單 |

  @diagnostics
  Scenario: 診斷輸出在成功、重送與拒絕流程中不含 Token、簽章或私鑰
    Given 診斷紀錄擷取已於獨立執行環境開始
    And 接入呼叫端 "orders-client" 以 mTLS 取得 Token
    And 接入呼叫端以 "orders-client" 簽章送出建立訂單 "diag-ref-1"，Idempotency-Key 為 "diag-key-1"
    When 接入呼叫端重送同一份已簽章的建立訂單請求
    Then 接入回應為 401
    And 診斷紀錄擷取已停止並寫完
    And 診斷輸出包含業務 API 的請求紀錄
    And 診斷輸出不含本次取得的 access_token
    And 診斷輸出不含簽章標頭值與私鑰標記

  Scenario: 重現說明涵蓋 Client 登錄、憑證與簽章配置、取得 Token、呼叫、重試、輪替與洩漏撤銷
    Given 10 單的實作紀錄可讀取
    Then 10 單的實作紀錄包含 "Client 登錄"
    And 10 單的實作紀錄包含 "憑證與簽章配置"
    And 10 單的實作紀錄包含 "取得 Token"
    And 10 單的實作紀錄包含 "呼叫業務 API"
    And 10 單的實作紀錄包含 "重試"
    And 10 單的實作紀錄包含 "正常輪替"
    And 10 單的實作紀錄包含 "洩漏撤銷"
    And 10 單的實作紀錄包含 "dotnet test"

  Scenario: 已確認產品參數與未完成事項有紀錄，且不以示範代替生產保證
    Given 10 單的實作紀錄可讀取
    Then 10 單的實作紀錄包含 "OpenIddict 7.7.1"
    And 10 單的實作紀錄包含 "Token 效期：300 秒"
    And 10 單的實作紀錄包含 "未完成事項"
    And 10 單的實作紀錄包含 "不宣稱跨系統 exactly-once"
    And 10 單的實作紀錄包含 "不代表生產容量"

  Scenario: 建置與測試的實際執行結果已記錄
    Given 10 單的實作紀錄可讀取
    Then 10 單的實作紀錄包含 "dotnet build"
    And 10 單的實作紀錄包含 "0 Error(s)"
    And 10 單的實作紀錄包含 "dotnet test"
    And 10 單的實作紀錄包含 "Passed!"

  Scenario: 實作紀錄與文件不含真實私鑰或憑證內容
    Given 10 單的實作紀錄可讀取
    Then 10 單的實作紀錄不包含 "-----BEGIN"

  Scenario: 10 單所有驗收項目已勾選且狀態為 resolved
    Given 10 單的實作紀錄可讀取
    Then 10 單狀態為 "resolved"
    And 10 單所有驗收項目已勾選
