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

  # Copy Structure

  Scenario: Create tournament by copying structure from another tournament
    Given the user is authenticated
    And a tournament "Summer Cup" exists with:
      | Discipline   | Volleyball |
      | GameLength   | 60         |
      | StartTime    | 2026-06-01 |
    And "Summer Cup" has 3 courts named "Main", "Side A", "Side B"
    And "Summer Cup" has 8 real teams
    And "Summer Cup" has a structure with:
      | Phase        | Group Stage | Format: RoundRobin | Groups: 2 | GroupWinners: 2 | StartTime: 09:00 |
      | Phase        | Playoffs    | Format: PlayoffElimination | Groups: 1 | StartTime: 14:00 |
    When the user creates a tournament "Winter Cup" copying structure from "Summer Cup"
    Then a new tournament "Winter Cup" is created
    And the tournament has discipline "Volleyball", game length 60, and start time "2026-06-01"
    And the tournament has 3 courts named "Main", "Side A", "Side B"
    And the tournament has 8 seed teams (Seed 1 through Seed 8) with graduated levels
    And the structure has 2 phases matching the source layout
    And games are generated and scheduled for all phases

  Scenario: Copied tournament gets seed teams instead of real teams
    Given the user is authenticated
    And a tournament "Source" exists with 6 real teams and 4 placeholder teams
    When the user creates a tournament "Copy" copying structure from "Source"
    Then "Copy" has 6 seed teams (not 10)
    And no real team names or contact info are copied

  Scenario: Copied tournament does not include volunteers
    Given the user is authenticated
    And a tournament "Source" exists with volunteers
    When the user creates a tournament "Copy" copying structure from "Source"
    Then "Copy" has no volunteers

  Scenario: Copied tournament gets a fresh tournament code
    Given the user is authenticated
    And a tournament "Source" exists with tournament code "AB12"
    When the user creates a tournament "Copy" copying structure from "Source"
    Then "Copy" has a different tournament code than "AB12"

  Scenario: Phase statuses after copy
    Given the user is authenticated
    And a tournament "Source" exists with 2 phases and full structure
    When the user creates a tournament "Copy" copying structure from "Source"
    Then phase 1 of "Copy" has status "InProgress"
    And phase 2 of "Copy" has status "Scheduled"

  Scenario: Placeholder teams generated for phase 2+ during copy
    Given the user is authenticated
    And a tournament "Source" exists with:
      | Phase | Group Stage | GroupWinners: 2 | Groups: 2 |
      | Phase | Playoffs    | Groups: 1                   |
    When the user creates a tournament "Copy" copying structure from "Source"
    Then "Copy" phase 2 has 4 placeholder teams from phase 1 progression

  Scenario: Copy skips game generation when prerequisites are missing
    Given the user is authenticated
    And a tournament "Source" exists with a phase that has no start time
    When the user creates a tournament "Copy" copying structure from "Source"
    Then the phase in "Copy" has no games generated
    And the phase status is "New"

  Scenario: Any user can copy from any tournament
    Given user "Alice" owns tournament "Cup A"
    And user "Bob" is authenticated
    When Bob creates a tournament "Cup B" copying structure from "Cup A"
    Then "Cup B" is created successfully owned by Bob
