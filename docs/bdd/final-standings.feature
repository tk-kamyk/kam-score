Feature: Final Standings (Last Phase)
  As a tournament viewer
  I want to see the standings of the last phase of the tournament
  So that I can see where each team finished

  Background:
    Given a tournament with a structure, phases, groups, and assigned teams

  # --- Single Phase ---

  Scenario: Single phase tournament uses group standings directly
    Given a single round-robin phase with 1 group containing teams "Eagles", "Hawks", "Wolves"
    And all games in the phase are completed
    And the phase is completed
    When I request the final standings
    Then the final standings should match the group standings:
      | Position | Team   |
      | 1        | Eagles |
      | 2        | Hawks  |
      | 3        | Wolves |

  Scenario: Single phase with multiple groups merges by cross-group ranking
    Given a single round-robin phase with 2 groups
    And group "A" contains "Eagles", "Hawks" and group "B" contains "Wolves", "Bears"
    And all games are completed and the phase is completed
    When I request the final standings
    Then all 4 teams appear in the final standings ranked by cross-group comparison

  # --- Multi-Phase ---

  Scenario: Multi-phase tournament shows only last phase teams
    Given Phase 1 (round-robin) with 8 teams across 2 groups, GroupWinners=2
    And Phase 2 (playoff) with 4 advancing teams
    And all games in both phases are completed and both phases are completed
    When I request the final standings
    Then only the 4 teams from Phase 2 appear in the standings
    And positions are 1-4 based on Phase 2 standings
    And the 4 teams eliminated in Phase 1 do not appear

  # --- Levels ---

  Scenario: Last phase with levels produces per-level standings
    Given Phase 1 with 2 groups, no levels
    And Phase 2 with 2 levels ("Gold", "Silver"), 1 group per level
    And all games are completed and all phases are completed
    When I request the final standings
    Then the response contains separate standings for "Gold" and "Silver"
    And each level has its own positions starting from 1
    And Gold standings only include teams from Gold-level groups
    And Silver standings only include teams from Silver-level groups

  Scenario: Last phase without levels produces flat standings
    Given a multi-phase tournament where the last phase has no levels
    And all games are completed and all phases are completed
    When I request the final standings
    Then the response contains a single flat list with no level names
    And positions are 1 through N for all teams in the last phase

  # --- Not Completed ---

  Scenario: Last phase not completed returns empty standings
    Given Phase 1 (completed) with 8 teams, 4 advance to Phase 2
    And Phase 2 is in progress with some games completed
    When I request the final standings
    Then the response is an empty list

  Scenario: No phases exist returns empty standings
    Given a tournament with no phases
    When I request the final standings
    Then the response is an empty list

  # --- Placeholder Exclusion ---

  Scenario: Placeholder teams are excluded from final standings
    Given Phase 2 (last phase) with 2 real teams and 2 placeholder teams
    And the phase is completed
    When I request the final standings
    Then only the 2 real teams appear in the standings
    And no placeholder team names appear

  # --- Edge Cases ---

  Scenario: No games completed in last phase returns empty standings
    Given the last phase is completed but has no completed games
    When I request the final standings
    Then the response is an empty list

  # --- API Access ---

  Scenario: Anonymous user can view final standings
    Given a tournament with the last phase completed
    When an anonymous user requests the final standings
    Then the standings should be returned successfully

  Scenario: Final standings for nonexistent tournament returns 404
    When I request final standings for a nonexistent tournament
    Then I should receive a 404 Not Found response
