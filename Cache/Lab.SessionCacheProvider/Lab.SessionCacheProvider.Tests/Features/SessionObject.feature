Feature: SessionObject

    As a developer
    I want to use SessionObject like ASP.NET Session
    So that I can store and retrieve values using an indexer

    Scenario: Set and get a value via indexer
        Given a SessionObject instance
        When I set the value "John" for key "UserName"
        Then the value for key "UserName" should be "John"

    Scenario: Get a non-existing key returns null
        Given a SessionObject instance
        Then the value for key "NonExisting" should be null

    Scenario: Setting null removes the value
        Given a SessionObject instance
        And I set the value "John" for key "UserName"
        When I set null for key "UserName"
        Then the value for key "UserName" should be null

    Scenario: Remove a value
        Given a SessionObject instance
        And I set the value "John" for key "UserName"
        When I remove the key "UserName"
        Then the value for key "UserName" should be null

    Scenario: Set and get a typed value
        Given a SessionObject instance
        When I set the integer value 42 for key "Age"
        Then the integer value for key "Age" should be 42

    Scenario: Overwrite an existing value
        Given a SessionObject instance
        And I set the value "John" for key "UserName"
        When I set the value "Jane" for key "UserName"
        Then the value for key "UserName" should be "Jane"
