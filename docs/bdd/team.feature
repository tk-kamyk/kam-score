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

  Scenario: Anonymous visitor sees teams without contact info
    Given user "Alice" owns tournament "Summer Cup" with team "Eagles" (email: "eagles@example.com", phone: "+123456789")
    When a visitor requests the teams of "Summer Cup"
    Then the team list is returned successfully
    And the team email and phone are hidden

  Scenario: Updating a team with duplicate name is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with teams "Eagles" and "Hawks"
    When the user updates team "Hawks" name to "Eagles"
    Then the request is rejected with 400 Bad Request

  Scenario: Anonymous visitor cannot add a team
    Given a tournament "Summer Cup" exists
    When a visitor attempts to add a team to "Summer Cup"
    Then the request is rejected with 401 Unauthorized

  # --- Team Schedule & Participation ---

  Scenario: API returns games filtered by teamId
    Given a tournament with teams "Eagles" and "Hawks" and "Wolves"
    And a phase with scheduled games including "Eagles" vs "Hawks" and "Hawks" vs "Wolves"
    When a user requests games filtered by teamId for "Hawks"
    Then the response contains games where "Hawks" plays (home or away) or referees
    And the response does not contain games where "Hawks" is not involved

  Scenario: Game response includes phase, group, and level names
    Given a tournament with a phase "Group Stage" (RoundRobin) with group "A" in level "Main"
    And scheduled games in that group
    When a user requests the games
    Then each game includes phaseName "Group Stage", groupName "A", and levelName "Main"

  Scenario: Team schedule shows games grouped by phase
    Given a tournament with teams assigned to multiple phases
    And games scheduled across those phases
    When a user expands a team row in the team list
    Then the team's games are displayed grouped under phase headers
    And each header shows the phase name, format, and group name

  Scenario: Team schedule highlights team's role in each game
    Given a tournament with "Eagles" playing home, away, and refereeing in different games
    When a user expands the "Eagles" team row
    Then each game row shows the team's role: "Home", "Away", or "Referee"

  Scenario: Team schedule shows breaks when toggled on
    Given a tournament with scheduled games across 5 time slots
    And "Eagles" is involved in 3 of those 5 time slots
    When a user expands "Eagles" and enables "Show breaks"
    Then 2 break rows are displayed for the time slots where "Eagles" has no game

  Scenario: Team with no scheduled games shows empty message
    Given a tournament with team "Eagles" assigned to no games
    When a user expands the "Eagles" team row
    Then the message "No games scheduled for this team" is displayed

  # --- Generate Seed Teams ---

  Scenario: Owner generates seed teams for an empty tournament
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with no teams
    When the user generates 4 seed teams
    Then the tournament has 4 teams
    And the teams are named "Seed 1", "Seed 2", "Seed 3", "Seed 4"
    And the team levels are proportionally distributed: 100, 67, 33, 0

  Scenario: Generating seed teams is additive
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with 2 existing teams
    When the user generates 3 seed teams
    Then the tournament has 5 teams total
    And the new teams are named "Seed 3", "Seed 4", "Seed 5"

  Scenario: Generating a single seed team assigns level 50
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with no teams
    When the user generates 1 seed team
    Then the tournament has 1 team named "Seed 1" with level 50

  Scenario: Seed team count must be between 1 and 100
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user attempts to generate 0 seed teams
    Then the request is rejected with 400 Bad Request
    When the user attempts to generate 101 seed teams
    Then the request is rejected with 400 Bad Request

  Scenario: Non-owner cannot generate seed teams
    Given user "Alice" owns tournament "Summer Cup"
    When user "Bob" attempts to generate seed teams for "Summer Cup"
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor cannot generate seed teams
    Given a tournament "Summer Cup" exists
    When a visitor attempts to generate seed teams for "Summer Cup"
    Then the request is rejected with 401 Unauthorized

  Scenario: Generated seed teams are real teams, not placeholders
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user generates 2 seed teams
    Then each generated team has isPlaceholder false
    And each generated team can be edited like a normal team
