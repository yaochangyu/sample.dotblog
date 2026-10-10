Feature: 憑證綁定的 Opaque Token 呼叫建立訂單
  服務以 mTLS 用戶端憑證向授權伺服器取得綁定該憑證的 Opaque Token，
  建立訂單 API 只接受同一張憑證搭配該 Token 的呼叫。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario Outline: 無法取得 Token 的呼叫端
    When 呼叫端以 "<憑證>" 向授權伺服器要求 Token
    Then 授權伺服器不核發 Token

    Examples:
      | 憑證             |
      | 無憑證           |
      | 其他 CA 簽發的憑證 |
      | 未登錄的自簽憑證 |

  Scenario: 綁定憑證取得不透明 Token
    When 呼叫端以 "orders-client" 向授權伺服器要求 Token
    Then 授權伺服器核發 access_token
    And access_token 不是 JWT 格式
    And 內省結果的 cnf 憑證指紋與 "orders-client" 憑證一致

  Scenario: 同一憑證搭配 Token 成功建立訂單
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 201
    And 回應包含訂單編號

  Scenario Outline: 偷到 Token 但憑證不符無法建立訂單
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "<憑證>" 送出建立訂單請求
    Then 建立訂單 API 回應 401

    Examples:
      | 憑證             |
      | 無憑證           |
      | 其他 CA 簽發的憑證 |
      | billing-client   |
      | 未登錄的自簽憑證 |

  Scenario: API 不信任公開請求自行提供的憑證標頭
    Given 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "billing-client" 送出建立訂單請求，並附帶偽造的 X-Client-Cert-Sha256 標頭，標頭值為 "orders-client" 憑證指紋
    Then 建立訂單 API 回應 401
