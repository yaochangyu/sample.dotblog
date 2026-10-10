Feature: 業務 API 依已驗證 Client 限制可執行操作與業務資料範圍
  依 05 單驗收項目，確認每個已驗證 Client 只能執行核准的 scope 操作，並只能讀取或修改自己的訂單。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API；本票於受控測試環境驗證業務授權。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 05 單的實作紀錄可讀取

  Scenario: 兩個獨立 Client 的允許與拒絕情境
    Given 已登錄服務 "orders-partner-client" 與其用戶端憑證
    And 呼叫端 "orders-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-partner-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-client" 建立訂單 "甲方訂單"
    And 呼叫端 "orders-partner-client" 建立訂單 "乙方訂單"
    Then 呼叫端 "orders-client" 查詢訂單 "甲方訂單" 回應 200
    And 呼叫端 "orders-client" 查詢訂單 "乙方訂單" 回應 404
    And 05 單第 1 項驗收已勾選

  Scenario: 未核准的 scope 要求不核發 Token
    When 呼叫端 "orders-client" 要求授權範圍 "orders.read admin.all" 的 Token
    Then 授權伺服器拒絕授權範圍要求，錯誤為 "invalid_scope"
    And 05 單第 2 項驗收已勾選

  Scenario: 只有 orders.read 的有效 Token 不能取消訂單
    Given 呼叫端 "orders-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-client" 建立訂單 "甲方訂單"
    And 呼叫端 "orders-client" 以授權範圍 "orders.read" 取得 Token
    When 呼叫端 "orders-client" 取消訂單 "甲方訂單"
    Then 最近一次操作回應 403
    And 呼叫端 "orders-client" 查詢訂單 "甲方訂單" 的狀態為 "active"
    And 呼叫端 "orders-client" 查詢訂單 "甲方訂單" 回應 200
    And 05 單第 2 項驗收已勾選
    And 05 單第 6 項驗收已勾選

  Scenario: 同 scope 的其他合作廠商不能取消他人訂單
    Given 已登錄服務 "orders-partner-client" 與其用戶端憑證
    And 呼叫端 "orders-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-partner-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-client" 建立訂單 "甲方訂單"
    When 呼叫端 "orders-partner-client" 取消訂單 "甲方訂單"
    Then 最近一次操作回應 404
    And 呼叫端 "orders-client" 查詢訂單 "甲方訂單" 的狀態為 "active"
    And 05 單第 3 項驗收已勾選
    And 05 單第 4 項驗收已勾選

  Scenario: 同 scope 的其他合作廠商不能讀取他人訂單
    Given 已登錄服務 "orders-partner-client" 與其用戶端憑證
    And 呼叫端 "orders-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-partner-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-client" 建立訂單 "甲方訂單"
    Then 呼叫端 "orders-partner-client" 查詢訂單 "甲方訂單" 回應 404
    And 05 單第 3 項驗收已勾選
    And 05 單第 4 項驗收已勾選

  Scenario: Body、查詢參數與路徑不能擴大已核准的資料範圍
    Given 已登錄服務 "orders-partner-client" 與其用戶端憑證
    And 呼叫端 "orders-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-partner-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-partner-client" 建立訂單 "乙方訂單"
    When 呼叫端 "orders-client" 建立訂單 "丙方訂單"，Body 與查詢參數宣稱 clientId 為 "orders-partner-client"
    Then 最近一次操作回應 201
    And 呼叫端 "orders-client" 查詢訂單 "丙方訂單" 的 clientId 為 "orders-client"
    And 呼叫端 "orders-client" 查詢訂單 "乙方訂單" 回應 404
    And 05 單第 5 項驗收已勾選

  Scenario: 授權依已驗證 Client 身分判斷，外部宣稱的身分標頭不影響授權且被拒絕的寫入不產生副作用
    Given 已登錄服務 "orders-partner-client" 與其用戶端憑證
    And 呼叫端 "orders-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-partner-client" 以授權範圍 "orders.read orders.write" 取得 Token
    And 呼叫端 "orders-client" 建立訂單 "甲方訂單"
    When 呼叫端 "orders-partner-client" 取消訂單 "甲方訂單"，並附帶 X-Client-Id 標頭 "orders-client"
    Then 最近一次操作回應 404
    And 呼叫端 "orders-client" 查詢訂單 "甲方訂單" 的狀態為 "active"
    When 呼叫端 "orders-client" 取消訂單 "甲方訂單"，並附帶 X-Client-Id 標頭 "orders-partner-client"
    Then 最近一次操作回應 200
    And 呼叫端 "orders-client" 查詢訂單 "甲方訂單" 的狀態為 "cancelled"
    And 05 單第 6 項驗收已勾選

  @record
  Scenario: 授權資料來源、契約與正向及越權整合情境可重現
    Then 05 單的實作紀錄包含 "授權資料來源"
    And 建立訂單契約檔包含 "/orders/{orderId}/cancel"
    And 05 單的實作紀錄包含 "dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj"
    And 05 單的實作紀錄包含 "越權"
    And 05 單第 7 項驗收已勾選

  @record
  Scenario: 本票為受控測試環境驗證，不視為正式接入完成
    Then 05 單的實作紀錄包含 "受控測試環境"
    And 05 單的實作紀錄包含 "不視為正式接入完成"
    And 05 單第 8 項驗收已勾選
