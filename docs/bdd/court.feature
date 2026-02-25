Feature: Court Management

  Scenario: Owner adds a court to a tournament
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user adds a court named "Court A"
    Then the court is added to the tournament
    And the tournament's court count is 1

  Scenario: Owner renames a court
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with court "Court A"
    When the user renames court "Court A" to "Main Court"
    Then the court name is "Main Court"

  Scenario: Owner deletes a court
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with court "Court A"
    When the user deletes court "Court A"
    Then the tournament's court count is 0

  Scenario: Adding a court with duplicate name is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with court "Court A"
    When the user adds a court named "Court A"
    Then the request is rejected with 400 Bad Request

  Scenario: Non-owner cannot add a court
    Given user "Alice" owns tournament "Summer Cup"
    When user "Bob" attempts to add a court to "Summer Cup"
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor can view courts
    Given user "Alice" owns tournament "Summer Cup" with court "Court A"
    When a visitor requests the courts of "Summer Cup"
    Then the court list is returned successfully

  Scenario: Anonymous visitor cannot add a court
    Given a tournament "Summer Cup" exists
    When a visitor attempts to add a court to "Summer Cup"
    Then the request is rejected with 401 Unauthorized
