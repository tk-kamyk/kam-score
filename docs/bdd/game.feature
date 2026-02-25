Feature: Game Management
  As a tournament owner
  I want to generate and manage games within groups
  So that teams can compete according to the tournament format

  Background:
    Given a tournament with structure, phases, groups, and seeded teams

  Scenario: Generate round robin games for a group
    Given group "A" has teams "Eagles", "Hawks", "Wolves"
    When I generate round robin games for group "A"
    Then 3 games should be created (Eagles-Hawks, Eagles-Wolves, Hawks-Wolves)
    And all games should have status "Scheduled"

  Scenario: Generate round robin games with 4 teams
    Given group "A" has 4 teams
    When I generate round robin games
    Then 6 games should be created (each team plays every other team once)

  Scenario: Cannot generate games twice
    Given games have already been generated for group "A"
    When I try to generate games again
    Then I should receive an error "games already exist"

  Scenario: Record game result as owner
    Given a scheduled game exists between "Eagles" and "Hawks"
    When the owner records result: 25-20, 25-22
    Then the game status should be "Completed"
    And the result should show Eagles winning 2-0

  Scenario: Record game result with set scores
    Given a scheduled game exists
    When the owner records result: 25-20, 20-25, 15-10
    Then the result should show 3 sets played

  Scenario: Clear game result
    Given a completed game exists
    When the owner clears the result
    Then the game status should return to "Scheduled"

  Scenario: Assign court to game
    Given a scheduled game and a court "Court 1"
    When the owner assigns "Court 1" to the game
    Then the game should show court "Court 1"

  Scenario: Assign referee team to game
    Given a scheduled game and a team "Wolves" as available referee
    When the owner assigns "Wolves" as referee
    Then the game should show "Wolves" as referee

  Scenario: Get games for a group
    Given group "A" has generated games
    When I request games for group "A"
    Then I should see all games with teams, status, court, and referee info

  Scenario: Anonymous user can view games
    Given games exist in a group
    When an anonymous visitor requests games
    Then they should see all games and results
