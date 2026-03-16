Feature: Final Standings (Tournament-wide)
  As a tournament viewer
  I want to see the overall tournament standings across all phases
  So that I can see where each team finished in the tournament

  Background:
    Given a tournament with a structure, phases, groups, and assigned teams

  # --- Single Phase ---

  Scenario: Single phase tournament uses group standings directly
    Given a single round-robin phase with 1 group containing teams "Eagles", "Hawks", "Wolves"
    And all games in the phase are completed
    When I request the final standings
    Then the final standings should match the group standings:
      | Position | Team   |
      | 1        | Eagles |
      | 2        | Hawks  |
      | 3        | Wolves |

  Scenario: Single phase with multiple groups merges by cross-group ranking
    Given a single round-robin phase with 2 groups
    And group "A" contains "Eagles", "Hawks" and group "B" contains "Wolves", "Bears"
    And all games are completed
    When I request the final standings
    Then all 4 teams appear in the final standings ranked by cross-group comparison

  # --- Multi-Phase ---

  Scenario: Multi-phase tournament ranks eliminated teams after advancing teams
    Given Phase 1 (round-robin) with 8 teams across 2 groups, GroupWinners=2
    And Phase 2 (playoff) with 4 advancing teams
    And all games in both phases are completed
    When I request the final standings
    Then positions 1-4 come from Phase 2 standings
    And positions 5-8 come from Phase 1 non-advancing teams ranked by cross-group standings

  Scenario: Three-phase tournament with progressive elimination
    Given Phase 1 with 8 teams, 4 advance to Phase 2
    And Phase 2 with 4 teams, 2 advance to Phase 3
    And Phase 3 with 2 teams (final)
    And all games in all phases are completed
    When I request the final standings
    Then positions 1-2 come from Phase 3
    And positions 3-4 come from Phase 2 non-advancing teams
    And positions 5-8 come from Phase 1 non-advancing teams

  Scenario: Non-advancing teams are ranked by cross-group standings within their phase
    Given Phase 1 with 2 groups of 4 teams each, GroupWinners=1
    And in group "A": "Eagles" (1st), "Hawks" (2nd), "Wolves" (3rd), "Bears" (4th)
    And in group "B": "Lions" (1st), "Tigers" (2nd), "Panthers" (3rd), "Falcons" (4th)
    And Phase 2 with "Eagles" and "Lions" advancing
    And all games are completed
    When I request the final standings
    Then positions 3-8 are assigned to the 6 non-advancing teams
    And they are ordered by cross-group comparison (points, set difference, point difference)

  # --- Levels ---

  Scenario: Tournament with levels produces per-level final standings
    Given Phase 1 with 2 levels ("Gold", "Silver"), 2 groups per level, 4 teams per group
    And Phase 2 with 2 levels, GroupWinners=1 from Phase 1
    And all games are completed
    When I request the final standings
    Then the response contains separate standings for "Gold" and "Silver"
    And each level has its own positions starting from 1
    And Gold standings only include teams from Gold-level groups
    And Silver standings only include teams from Silver-level groups

  Scenario: Tournament without levels produces flat standings
    Given a multi-phase tournament with no levels defined
    And all games are completed
    When I request the final standings
    Then the response contains a single flat list with no level names
    And positions are 1 through N for all teams

  # --- Provisional Standings ---

  Scenario: Provisional standings when last phase is in progress
    Given Phase 1 (completed) with 8 teams, 4 advance to Phase 2
    And Phase 2 is in progress with some games completed
    When I request the final standings
    Then the standings are marked as "provisional"
    And positions 5-8 are finalized (from Phase 1 eliminated teams)
    And positions 1-4 reflect current Phase 2 standings

  Scenario: Provisional standings when only first phase has games
    Given Phase 1 is in progress with some games completed
    And Phase 2 has no games yet
    When I request the final standings
    Then all teams appear with current Phase 1 standings
    And the standings are marked as "provisional"

  Scenario: Final standings are not provisional when all phases are completed
    Given all phases in the tournament are completed
    When I request the final standings
    Then the standings are not marked as "provisional"

  # --- Placeholder Exclusion ---

  Scenario: Placeholder teams are excluded from final standings
    Given Phase 1 with 4 real teams and Phase 2 with 2 placeholder teams
    And Phase 1 is completed but Phase 2 has not started
    When I request the final standings
    Then only the 4 real teams appear in the standings
    And no placeholder team names appear

  # --- Edge Cases ---

  Scenario: No games completed returns empty standings
    Given a tournament with phases but no completed games
    When I request the final standings
    Then the response is an empty list

  Scenario: Phase without progression config is the last phase
    Given Phase 1 with no GroupWinners and no TotalTeamsProceeding
    And all Phase 1 games are completed
    When I request the final standings
    Then all teams are ranked from Phase 1 standings

  # --- API Access ---

  Scenario: Anonymous user can view final standings
    Given a tournament with completed games
    When an anonymous user requests the final standings
    Then the standings should be returned successfully

  Scenario: Final standings for nonexistent tournament returns 404
    When I request final standings for a nonexistent tournament
    Then I should receive a 404 Not Found response
