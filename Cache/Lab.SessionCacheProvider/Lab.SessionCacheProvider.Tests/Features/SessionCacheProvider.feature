Feature: SessionCacheProvider

    As a developer
    I want SessionCacheProvider to manage session ID via cookies
    So that I can use HybridCache as a session store without request queuing

    Scenario: Create a new session ID when no cookie exists
        Given an HTTP request without a session cookie
        When I access the Session property
        Then a new session ID cookie should be created
        And the Session property should return a SessionObject

    Scenario: Reuse existing session ID from cookie
        Given an HTTP request with session cookie "abc123"
        When I access the Session property
        Then the session ID should be "abc123"

    Scenario: Store and retrieve value through SessionCacheProvider
        Given an HTTP request without a session cookie
        When I set "Hello" for key "Greeting" through the Session property
        Then the value for key "Greeting" through the Session property should be "Hello"
