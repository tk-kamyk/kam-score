Feature: Tournament Management

  Scenario: Authenticated user creates a tournament
    Given the user is authenticated
    When the user creates a tournament with:
      | Name       | Summer Cup    |
      | Discipline | Volleyball    |
      | GameLength | 60            |
    Then the tournament is created successfully
    And the tournament has an auto-generated code of 4 hex characters
    And the tournament appears in the user's tournament list

  Scenario: Tournament code is visible to owner
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user requests the tournament details
    Then the tournament code is visible in the response

  Scenario: Tournament code is not visible to anonymous visitors
    Given a tournament "Summer Cup" exists
    When a visitor requests the tournament details
    Then the tournament code is not visible in the response

  Scenario: User creates a tournament with game conditions
    Given the user is authenticated
    When the user creates a tournament with:
      | Name         | Beach Cup        |
      | Discipline   | BeachVolleyball  |
      | BestOfSets   | 3                |
      | PointsPerSet | 21,21,15         |
      | GameLength   | 45               |
    Then the tournament game conditions are stored correctly

  Scenario: User updates a tournament
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user updates the tournament name to "Winter Cup"
    Then the tournament name is "Winter Cup"

  Scenario: User deletes a tournament
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user deletes the tournament
    Then the tournament no longer appears in the list

  Scenario: Authenticated user sees all tournaments
    Given user "Alice" owns tournament "Cup A"
    And user "Bob" owns tournament "Cup B"
    When Alice requests the tournament list
    Then both "Cup A" and "Cup B" are returned

  Scenario: Authenticated user sees codes only for own tournaments in list
    Given user "Alice" owns tournament "Cup A"
    And user "Bob" owns tournament "Cup B"
    When Alice requests the tournament list
    Then "Cup A" includes the tournament code
    And "Cup B" does not include the tournament code

  Scenario: Anonymous visitor sees all tournaments without codes in list
    Given user "Alice" owns tournament "Cup A"
    And user "Bob" owns tournament "Cup B"
    When a visitor requests the tournament list
    Then both "Cup A" and "Cup B" are returned
    And neither tournament includes the tournament code

  Scenario: User cannot update another user's tournament
    Given user "Alice" owns tournament "Cup A"
    When user "Bob" attempts to update "Cup A"
    Then the request is rejected with 403 Forbidden

  Scenario: User cannot delete another user's tournament
    Given user "Alice" owns tournament "Cup A"
    When user "Bob" attempts to delete "Cup A"
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor can view any tournament
    Given user "Alice" owns tournament "Cup A"
    When a visitor requests the details of "Cup A"
    Then the tournament details are returned successfully
