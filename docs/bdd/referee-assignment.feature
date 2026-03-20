Feature: Manual Referee Assignment
  As a tournament owner
  I want to manually assign a referee to a game that has no referee
  So that every game has a designated referee even when auto-assignment leaves gaps

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with courts, teams, groups, and scheduled games

  # --- Candidate List ---

  Scenario: Candidate list includes teams from same level across groups
    Given a phase with 2 levels, each with 2 groups of 4 teams
    And games are generated and scheduled
    And a game in Level 1 Group A has no referee assigned
    When I request referee candidates for that game
    Then I should see teams from both Level 1 Group A and Level 1 Group B
    And I should not see teams from Level 2

  Scenario: Candidate list includes all phase teams when no levels defined
    Given a phase with no levels and 2 groups of 4 teams
    And games are generated and scheduled
    And a game in Group A has no referee assigned
    When I request referee candidates for that game
    Then I should see teams from both Group A and Group B

  Scenario: Candidate list excludes teams playing in the same time slot
    Given a game at 10:00 with no referee
    And another game at 10:00 where Team X is playing
    When I request referee candidates for the first game
    Then Team X should not be in the candidate list

  Scenario: Candidate list excludes teams refereeing in the same time slot
    Given a game at 10:00 with no referee
    And another game at 10:00 where Team Y is already assigned as referee
    When I request referee candidates for the first game
    Then Team Y should not be in the candidate list

  Scenario: Candidate list excludes teams playing in the next time slot
    Given a game at 10:00 with no referee and game length of 30 minutes
    And a game at 10:30 where Team Z is playing
    When I request referee candidates for the 10:00 game
    Then Team Z should not be in the candidate list

  Scenario: Candidate list excludes the home and away teams of the target game
    Given a game between Team A (home) and Team B (away) with no referee
    When I request referee candidates for that game
    Then Team A should not be in the candidate list
    And Team B should not be in the candidate list

  Scenario: Placeholder teams are included as candidates
    Given a playoff phase with placeholder teams (e.g., "Loser QF1")
    And a game has no referee assigned
    And the placeholder team is free in the current and next time slot
    When I request referee candidates for that game
    Then the placeholder team should appear in the candidate list

  # --- Assignment ---

  Scenario: Owner assigns a referee to a game without one
    Given a game with no referee assigned
    And Team C is a valid candidate
    When I assign Team C as referee for that game
    Then the game should have Team C as the referee
    And the game's referee name should be visible in the schedule

  Scenario: Assigning an ineligible team is rejected
    Given a game with no referee assigned
    And Team X is playing in the same time slot
    When I try to assign Team X as referee for that game
    Then I should receive a 400 error

  # --- Access Control ---

  Scenario: Non-owner cannot assign a referee
    Given a game with no referee in someone else's tournament
    When I try to assign a referee for that game
    Then I should receive a 403 Forbidden error

  Scenario: Non-owner cannot view referee candidates
    Given a game in someone else's tournament
    When I try to request referee candidates for that game
    Then I should receive a 403 Forbidden error
