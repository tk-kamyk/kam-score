Feature: Auto-Scheduling
  As a tournament organizer
  I want games to be automatically scheduled across courts and time slots
  So that teams have fair play time, adequate rest, and courts are utilized evenly

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with courts, teams, groups, and a configured game length

  # --- Core Scheduling Constraints ---

  Scenario: Auto-schedule distributes all games across time slots and courts
    Given a phase with 1 group of 4 teams (6 games) and 2 courts
    When I generate and schedule games for the phase
    Then all 6 games should have a start time and court assigned
    And games should be distributed across both courts

  Scenario: No team plays or referees two games at the same time
    Given a phase with 1 group of 4 teams (6 games) and 2 courts
    When I generate and schedule games for the phase
    Then no team should appear in two games scheduled at the same time (as player or referee)

  Scenario: No team referees two consecutive time slots
    Given a phase with 1 group of 4 teams (games with auto-referee) and 1 court
    When I generate and schedule games for the phase
    Then no team should referee in two consecutive time slots

  Scenario: Team must have a free time slot before playing
    Given a phase with 1 group of 4 teams (games with auto-referee) and 1 court
    When I generate and schedule games for the phase
    Then no team should be active (playing or refereeing) in the time slot immediately before a game they play in

  Scenario: Referees assigned after scheduling from same group
    Given a phase with 1 group of 4 teams and 1 court
    When I generate and schedule games for the phase
    Then every round-robin game should have a referee assigned
    And no referee should be a team playing in that game
    And referee duties should be balanced across teams in the group

  Scenario: Games interleaved across groups within a phase
    Given a phase with 2 groups of 3 teams each (3 games per group, 6 total) and 2 courts
    When I generate and schedule games for the phase
    Then games from both groups should be interleaved, not all of one group first

  Scenario: Courts are utilized uniformly
    Given a phase with 1 group of 4 teams (6 games) and 3 courts
    When I generate and schedule games for the phase
    Then each court should have at most 1 more game than any other court

  # --- Playoff Round Ordering ---

  Scenario: Playoff rounds are scheduled in chronological order
    Given a playoff-elimination phase with 4 teams and 2 courts
    When I generate and schedule games for the phase
    Then all semifinal games should be scheduled before the final
    And the final should start after all semifinals have completed

  Scenario: Placement games ordered worst-to-best with Final last
    Given a playoff-with-placement phase with 8 teams and 2 courts
    When I generate and schedule games for the phase
    Then consolation rounds should be scheduled before main bracket rounds at each level
    And placement games should be ordered worst-to-best position (7th before 5th before 3rd)
    And the Final should always be the last game scheduled

  # --- Edge Cases ---

  Scenario: Schedule with single court creates sequential slots
    Given a phase with 3 games and 1 court
    When I generate and schedule games for the phase
    Then all games should be on the same court in sequential time slots

  Scenario: Schedule with no games should fail
    Given a phase with no teams in any group
    When I try to generate and schedule games for the phase
    Then I should receive an error

  Scenario: Schedule with no courts should fail
    Given a tournament with no courts
    When I try to generate and schedule games for the phase
    Then I should receive an error "At least one court is required"

  # --- Access Control ---

  Scenario: Anonymous user can view schedule
    Given a phase with scheduled games
    When an anonymous user requests the games
    Then they should see the full schedule

  Scenario: Only owner can run generate and schedule
    Given a phase in someone else's tournament
    When I try to generate and schedule games
    Then I should receive a 403 Forbidden error
