Feature: CacheSession

    As a developer
    I want to use CacheSession.Current like native Session
    So that I can access session data with minimal code changes

    Scenario: Set and get a value via CacheSession.Current
        Given a configured CacheSession
        When I set "John" for key "UserName" via CacheSession.Current
        Then the value for key "UserName" via CacheSession.Current should be "John"

    Scenario: Get a non-existing key returns null
        Given a configured CacheSession
        Then the value for key "NonExisting" via CacheSession.Current should be null

    Scenario: Remove a value via CacheSession.Current
        Given a configured CacheSession
        And I set "John" for key "UserName" via CacheSession.Current
        When I remove the key "UserName" via CacheSession.Current
        Then the value for key "UserName" via CacheSession.Current should be null
