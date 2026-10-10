Feature: 呼叫服務取得 Token 並呼叫建立訂單 API 的保護邊界
  依 02 單驗收項目，確認 Token 生命週期、內省判斷、目標 API、已驗證 Client 身分與本 lab 的範圍限制。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario: 主要整合測試邊界為呼叫端直連業務 API
    Given 02 單的實作紀錄可讀取
    Then 02 單的實作紀錄包含 "主要整合測試邊界：呼叫端 → 業務 API（本 lab 不經 Gateway）"
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 201

  Scenario: 各示範 Client 使用獨立的用戶端憑證
    Then 各示範 Client 的用戶端憑證指紋彼此不同
    And 各示範 Client 的用戶端憑證含私鑰

  Scenario: 以 Client Secret 要求 Token 不核發
    When 呼叫端以 "orders-client" 的 client_id 附帶 client_secret 且不附憑證向授權伺服器要求 Token
    Then 授權伺服器不核發 Token

  Scenario: 以 API Key 呼叫建立訂單不被接受
    When 呼叫端以 API Key 送出建立訂單請求
    Then 建立訂單 API 回應 401

  Scenario: mTLS 認證成功後核發短效且綁定憑證的 Token
    When 呼叫端以 "orders-client" 向授權伺服器要求 Token
    Then 授權伺服器核發 access_token
    And 授權伺服器核發的 Token 效期為 300 秒

  @record
  Scenario: Token 效期的確認狀態有紀錄
    Given 02 單的實作紀錄可讀取
    Then 02 單的實作紀錄包含 "Token 效期：300 秒（使用者已核准）"
    And 02 單 mTLS 短效 Token 項目已勾選

  @short-lifetime
  Scenario: Token 到期後拒絕呼叫並可重新取得
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    Then 授權伺服器核發的 Token 效期為 2 秒
    When 等待 Token 到期
    And 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 401
    When 呼叫端以 "orders-client" 向授權伺服器要求 Token
    Then 授權伺服器核發 access_token
    When 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 201

  Scenario: 未附用戶端憑證的呼叫端不能查詢 Token 內省
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 未附用戶端憑證的呼叫端查詢 Token 內省
    Then 內省端點拒絕該呼叫

  Scenario: 內省回報有效、有效期限、目標 API 與綁定憑證
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 查詢 Token 內省
    Then 內省結果為 active
    And 內省結果的到期時間晚於現在
    And 內省結果的目標 API 為 "orders-api"
    And 內省結果的 cnf 憑證指紋與 "orders-client" 憑證一致

  Scenario: 其他目標 API 的 Token 不能呼叫建立訂單 API
    Given 呼叫端已以 "billing-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "billing-client" 送出建立訂單請求
    Then 建立訂單 API 回應 401

  Scenario: 無效 Token 不能呼叫建立訂單 API
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有無效 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 401

  Scenario: 建立訂單的 clientId 為已驗證 Client，不採信 Body 或外部標頭
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求，Body 與 X-Client-Id 標頭宣稱為 "billing-client"
    Then 建立訂單 API 回應 201
    And 回應 clientId 為 "orders-client"

  @record
  Scenario: Gateway 下游通道項目標註為本 lab 不實作且略過
    Given 02 單的實作紀錄可讀取
    Then 02 單的實作紀錄包含 "本 lab 不實作，略過"
    And 02 單 Gateway 項目標為略過而非完成

  @ignore
  Scenario: Gateway 下游通道經認證並覆寫偽造的驗證標頭（本 lab 不實作）
    Given 可信 Gateway 終止 mTLS 並轉送呼叫端憑證資訊
    When 呼叫端繞過 Gateway 直接送出建立訂單請求
    Then 業務 API 拒絕不可信的入口標頭

  @record
  Scenario: 示範流程可重現且明確標示尚未具備的保護
    Given 02 單的實作紀錄可讀取
    Then 02 單的實作紀錄包含 "dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj"
    And 02 單的實作紀錄包含 "尚未具備簽章"
    And 02 單的實作紀錄包含 "防重放"
    And 02 單的實作紀錄包含 "完整業務授權"
