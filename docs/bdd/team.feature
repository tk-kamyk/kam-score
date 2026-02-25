Feature: Team Management

  Scenario: Owner adds a team to a tournament
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user adds a team with:
      | Name    | Eagles  |
      | Level   | 75      |
      | Email   | eagles@example.com |
      | Phone   | +123456789 |
    Then the team is added to the tournament
    And the tournament's team count is 1

  Scenario: Owner updates a team
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with team "Eagles"
    When the user updates team "Eagles" name to "Hawks" and level to 80
    Then the team name is "Hawks"
    And the team level is 80

  Scenario: Owner deletes a team
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with team "Eagles"
    When the user deletes team "Eagles"
    Then the tournament's team count is 0

  Scenario: Adding a team with duplicate name is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with team "Eagles"
    When the user adds a team with name "Eagles"
    Then the request is rejected with 400 Bad Request

  Scenario: Team level must be between 0 and 100
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user adds a team with level 101
    Then the request is rejected with 400 Bad Request

  Scenario: Non-owner cannot add a team
    Given user "Alice" owns tournament "Summer Cup"
    When user "Bob" attempts to add a team to "Summer Cup"
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor can view teams
    Given user "Alice" owns tournament "Summer Cup" with team "Eagles"
    When a visitor requests the teams of "Summer Cup"
    Then the team list is returned successfully

  Scenario: Anonymous visitor cannot add a team
    Given a tournament "Summer Cup" exists
    When a visitor attempts to add a team to "Summer Cup"
    Then the request is rejected with 401 Unauthorized
