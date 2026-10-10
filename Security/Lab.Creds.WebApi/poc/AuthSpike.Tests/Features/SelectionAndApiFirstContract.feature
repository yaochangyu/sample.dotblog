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

  Scenario Outline: 管理介面與登錄申請端點已列入 API First 契約
    Then 建立訂單契約檔包含 "<路徑>"

    Examples:
      | 路徑                                                                 |
      | /admin/trust-list                                                    |
      | /admin/audit-records                                                 |
      | /admin/clients/{clientId}/disable                                    |
      | /admin/clients/{clientId}/certificates/{thumbprint}/retire           |
      | /admin/clients/{clientId}/certificates/{thumbprint}/revoke           |
      | /admin/clients/{clientId}/signing-keys/{keyId}/retire                |
      | /admin/clients/{clientId}/signing-keys/{keyId}/revoke                |
      | /client-certificate-requests                                         |
      | /admin/client-certificate-requests/{requestId}                       |
      | /admin/client-certificate-requests/{requestId}/approve               |
      | /admin/client-certificate-requests/{requestId}/reject                |
      | /signing-key-requests                                                |
      | /admin/signing-key-requests/{requestId}                              |
      | /admin/signing-key-requests/{requestId}/approve                      |
      | /admin/signing-key-requests/{requestId}/reject                       |
