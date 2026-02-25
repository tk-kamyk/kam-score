Feature: User Authentication

  Scenario: Visitor accesses public tournament data without authentication
    Given a tournament "Summer Cup" exists
    When a visitor requests the tournament list
    Then the tournament list is returned successfully

  Scenario: Visitor cannot create data without authentication
    When a visitor attempts to create a tournament
    Then the request is rejected with 401 Unauthorized

  Scenario: Visitor cannot update data without authentication
    Given a tournament "Summer Cup" exists
    When a visitor attempts to update the tournament
    Then the request is rejected with 401 Unauthorized

  Scenario: Visitor cannot delete data without authentication
    Given a tournament "Summer Cup" exists
    When a visitor attempts to delete the tournament
    Then the request is rejected with 401 Unauthorized

  Scenario: Authenticated user can create data
    Given the user is authenticated
    When the user creates a tournament with name "Summer Cup"
    Then the tournament is created successfully
