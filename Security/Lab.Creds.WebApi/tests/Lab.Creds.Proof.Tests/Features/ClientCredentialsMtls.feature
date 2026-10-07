Feature: mTLS Client Credentials 核發憑證綁定的 opaque token (Ticket 01 proof)
  作為合作廠商的呼叫服務
  我希望以已註冊的 TLS 憑證，透過 mTLS 取得綁定該憑證的 reference token
  以便不使用 Client Secret，且 token 不能被其他憑證冒用

  Scenario: 已註冊憑證以 mTLS 取得 reference token
    Given 呼叫服務 "partner-a" 持有已註冊的 TLS 憑證
    When 該服務以 mTLS 對 token 端點發出 client_credentials 請求
    Then token 端點回應 200 且 token_type 為 Bearer
    And access_token 為 opaque reference token 而非 JWT

  Scenario: 受認證的 resource server introspection 回傳憑證綁定 cnf
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When resource server "partner-api" 以其已註冊憑證 mTLS 對 introspection 端點查詢該 token
    Then introspection 回應 active 為 true
    And introspection 回應 cnf 的 x5t#S256 等於呼叫服務憑證的 SHA-256 指紋

  Scenario: 未註冊憑證不能呼叫 introspection
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有未註冊憑證的 "partner-api" 冒名對 introspection 端點查詢該 token
    Then introspection 端點拒絕請求

  Scenario: 沒有憑證不能呼叫 introspection
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 沒有出示任何憑證的 "partner-api" 對 introspection 端點查詢該 token
    Then introspection 端點拒絕請求

  Scenario: token 有效期為 5 分鐘
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    Then token 回應的 expires_in 介於 299 到 300 秒
    When resource server "partner-api" 以其已註冊憑證 mTLS 對 introspection 端點查詢該 token
    Then introspection 回應的 exp 減 iat 恰為 300 秒
