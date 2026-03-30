Feature: Device Fingerprint Authentication
  As a user
  I want to authenticate with device fingerprint verification
  So that my account is protected from unauthorized devices

  Background:
    Given a registered user "testuser" with password "password123"

  Scenario: Login from a known verified device returns JWT token
    Given the user has a verified device with fingerprint "device-abc-123"
    When the user logs in with username "testuser" password "password123" and fingerprint "device-abc-123"
    Then the response should contain a JWT token
    And requireDeviceVerification should be false

  Scenario: Login from a new device requires OTP verification
    When the user logs in with username "testuser" password "password123" and fingerprint "new-device-xyz"
    Then requireDeviceVerification should be true
    And the response should contain userId and fingerprintHash

  Scenario: Verify new device with valid OTP binds device and returns JWT
    Given the user logged in from new device with fingerprint "new-device-xyz"
    And an OTP was generated for the device
    When the user submits the correct OTP for device verification
    Then the response should contain a JWT token
    And the device should be marked as verified

  Scenario: Verify device with invalid OTP returns 401
    Given the user logged in from new device with fingerprint "new-device-xyz"
    When the user submits the wrong OTP "000000" for device verification
    Then the response status should be 401

  Scenario: Accessing protected endpoint with valid token and matching fingerprint succeeds
    Given the user has a verified device with fingerprint "device-abc-123"
    And the user is authenticated with fingerprint "device-abc-123"
    When the user calls GET /api/me with the correct fingerprint header
    Then the response status should be 200

  Scenario: Accessing protected endpoint with mismatched fingerprint returns 403
    Given the user has a verified device with fingerprint "device-abc-123"
    And the user is authenticated with fingerprint "device-abc-123"
    When the user calls GET /api/me with fingerprint header "wrong-device-999"
    Then the response status should be 403

  Scenario: Login with wrong password returns 401
    When the user logs in with username "testuser" password "wrongpass" and fingerprint "device-abc-123"
    Then the response status should be 401
