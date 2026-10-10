Feature: 授權伺服器選型與建立訂單 API First 契約
  以實際執行的版本與契約回應，確認已選定的 OpenIddict 候選與 API First 建立訂單契約。

  Scenario: OpenIddict 候選為正式版本並以 ASP.NET Core 10 執行
    Then OpenIddict 伺服器元件版本為 "7.7.1"
    And 本 spike 以 .NET 10 目標框架建置

  Scenario: 建立訂單回應符合 API First 契約
    Given 建立訂單契約 doc/openapi.yml 定義 POST /orders 回應 201 與 orderId
    And 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 201
    And 回應 Location 標頭以 "/orders/" 開頭
    And 回應 orderId 為契約定義的 uuid 格式
