Feature: Auto-Scheduling

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with courts, teams, groups, and a configured game length

  # Constraint violations are covered by unit tests (GameSchedulerTests).
  # These scenarios assert the contract surfaced through the API.

  Scenario: Auto-schedule assigns every game a court and start time
    Given a phase with multiple groups and multiple courts
    When I generate and schedule games for the phase
    Then all generated games have a court and start time assigned

  Scenario: No team plays or referees two games at the same time
    Given a phase with overlapping constraints possible
    When games are generated and scheduled
    Then no team is active (playing or refereeing) in two simultaneous games

  Scenario: Referees are auto-assigned and balanced across teams in the group
    Given a round-robin phase
    When games are generated and scheduled
    Then each game has a referee from outside the game
    And referee duties are balanced across the group

  Scenario: Games are interleaved across groups and courts are utilised uniformly
    Given a phase with multiple groups and courts
    When games are generated and scheduled
    Then games from different groups are interleaved in the schedule
    And courts differ by at most one game in their allocated count

  Scenario: Playoff rounds are scheduled in chronological round order
    Given a playoff phase (with or without placement games)
    When games are generated and scheduled
    Then earlier rounds are scheduled before later rounds
    And the Final is scheduled last when the format has placement games

  Scenario Outline: Schedule generation fails when prerequisites are missing
    Given the tournament is missing <prerequisite>
    When I try to generate and schedule games
    Then the request is rejected

    Examples:
      | prerequisite         |
      | any courts           |
      | any teams in groups  |

  Scenario: Anonymous user can view the schedule
    Given a phase with scheduled games
    When an anonymous user requests games
    Then the full schedule is returned

  Scenario: Only owner can generate and schedule games
    Given a phase in someone else's tournament
    When I try to generate and schedule games
    Then the request is rejected with status 403
