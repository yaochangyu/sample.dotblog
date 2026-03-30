Feature: 裝置指紋驗證
  As a 使用者
  I want to 透過裝置指紋進行身分驗證
  So that 帳號受到保護，不允許未綁定的裝置登入

  Background:
    Given 已建立帳號 "testuser" 密碼為 "password123"

  Scenario: 從已知驗證裝置登入，直接回傳 JWT
    Given 使用者已綁定指紋為 "device-abc-123" 的裝置
    When 使用者以帳號 "testuser" 密碼 "password123" 指紋 "device-abc-123" 登入
    Then 回應應包含 JWT token
    And requireDeviceVerification 應為 false

  Scenario: 從新裝置登入，需要 OTP 驗證
    When 使用者以帳號 "testuser" 密碼 "password123" 指紋 "new-device-xyz" 登入
    Then requireDeviceVerification 應為 true
    And 回應應包含 userId 與 fingerprintHash

  Scenario: 以正確 OTP 驗證新裝置，綁定裝置並回傳 JWT
    Given 使用者已從指紋 "new-device-xyz" 的新裝置登入
    And 系統已產生 OTP
    When 使用者提交正確的 OTP 進行裝置驗證
    Then 回應應包含 JWT token
    And 裝置應被標記為已驗證

  Scenario: 提交錯誤 OTP，回傳 401
    Given 使用者已從指紋 "new-device-xyz" 的新裝置登入
    When 使用者提交錯誤 OTP "000000" 進行裝置驗證
    Then 回應狀態碼應為 401

  Scenario: 使用正確 token 與符合指紋存取受保護端點，回傳 200
    Given 使用者已綁定指紋為 "device-abc-123" 的裝置
    And 使用者已以指紋 "device-abc-123" 完成認證
    When 使用者以正確指紋標頭呼叫 GET /api/me
    Then 回應狀態碼應為 200

  Scenario: 使用不符的指紋存取受保護端點，回傳 403
    Given 使用者已綁定指紋為 "device-abc-123" 的裝置
    And 使用者已以指紋 "device-abc-123" 完成認證
    When 使用者以指紋標頭 "wrong-device-999" 呼叫 GET /api/me
    Then 回應狀態碼應為 403

  Scenario: 密碼錯誤，回傳 401
    When 使用者以帳號 "testuser" 密碼 "wrongpass" 指紋 "device-abc-123" 登入
    Then 回應狀態碼應為 401
