Feature: 受保護 API 以憑證綁定判斷接受請求 (Ticket 01 proof)
  作為合作廠商提交資料 API 的維護者
  我希望 API 只接受「token 有效且綁定憑證等於本次 mTLS 憑證」的請求
  以便偷到 token 的人無法單憑 token 呼叫 API

  Scenario: 以取得 token 的同一憑證呼叫 API 成功
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務以同一憑證和 token 對 API 提交合作廠商資料
    Then API 回應 202 並回報已驗證 Client 為 "partner-a"

  Scenario: 有效 token 但沒有出示憑證被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 攻擊者只帶 token 且不出示憑證對 API 提交合作廠商資料
    Then API 回應 401

  Scenario: 有效 token 搭配另一張已註冊憑證被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有 "partner-b" 憑證的呼叫端帶著 partner-a 的 token 對 API 提交合作廠商資料
    Then API 回應 401

  Scenario: 有效 token 搭配未註冊憑證被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有未註冊憑證的呼叫端帶著 partner-a 的 token 對 API 提交合作廠商資料
    Then API 回應 401

  Scenario: 有憑證但沒有 token 被拒絕
    Given 呼叫服務 "partner-a" 持有已註冊的 TLS 憑證
    When 該服務只出示憑證而不帶 token 對 API 提交合作廠商資料
    Then API 回應 401

  Scenario: 沒有憑證不能向 token 端點取得 token
    Given 呼叫服務 "partner-a" 持有已註冊的 TLS 憑證
    When 沒有出示憑證的呼叫端以 partner-a 的 client_id 對 token 端點發出請求
    Then token 端點拒絕並回報 "invalid_request"

  Scenario: 未註冊憑證不能向 token 端點取得 token
    Given 呼叫服務 "partner-a" 持有已註冊的 TLS 憑證
    When 持有未註冊憑證的呼叫端以 partner-a 的 client_id 對 token 端點發出請求
    Then token 端點拒絕並回報 "invalid_client"

  Scenario: 撤銷 token 後 API 立即拒絕後續請求
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    And 該 token 目前可以成功呼叫 API
    When 授權伺服器撤銷該 token
    Then 同一憑證與 token 再次呼叫 API 回應 401
