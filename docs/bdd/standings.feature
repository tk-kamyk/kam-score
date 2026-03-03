Feature: Group Standings
  As a tournament viewer
  I want to see standings for each group
  So that I can track team performance and positions

  Background:
    Given a tournament with a structure, phases, groups, and assigned teams

  # --- Round Robin Standings ---

  Scenario: Round robin standings with all games completed
    Given a round-robin phase with group "A" containing teams "Eagles", "Hawks", "Wolves"
    And "Eagles" beat "Hawks" 2-1
    And "Eagles" beat "Wolves" 2-0
    And "Hawks" beat "Wolves" 2-1
    When I request standings for group "A"
    Then the standings should be ordered:
      | Position | Team   | GP | W | D | L | Pts | S+ | S- | S± |
      | 1        | Eagles | 2  | 2 | 0 | 0 | 4   | 4  | 1  | 3  |
      | 2        | Hawks  | 2  | 1 | 0 | 1 | 2   | 3  | 3  | 0  |
      | 3        | Wolves | 2  | 0 | 0 | 2 | 1   | 4  | -3 |

  Scenario: Round robin standings with a draw
    Given a round-robin phase with group "A" containing teams "Eagles", "Hawks"
    And "Eagles" drew with "Hawks" with a single-set score of 25-25
    When I request standings for group "A"
    Then both teams should have 1 point each
    And both teams should have 0 wins and 0 losses
    And both teams should have 1 draw

  Scenario: Round robin tiebreaker by set difference
    Given a round-robin phase with group "A" containing teams "Eagles", "Hawks", "Wolves"
    And "Eagles" beat "Hawks" 2-1
    And "Hawks" beat "Wolves" 2-0
    And "Wolves" beat "Eagles" 2-1
    When I request standings for group "A"
    Then all three teams should have 2 points
    And the team with the best set difference should be ranked first

  Scenario: Round robin tiebreaker by direct result
    Given a round-robin phase with group "A" containing teams "Eagles", "Hawks", "Wolves", "Bears"
    And "Eagles" and "Hawks" are tied on points and set difference
    When I request standings for group "A"
    Then the winner of the direct match between "Eagles" and "Hawks" should be ranked higher

  Scenario: Round robin no completed games
    Given a round-robin phase with group "A" containing teams "Eagles", "Hawks", "Wolves"
    And no games have been completed
    When I request standings for group "A"
    Then all teams should appear in the standings with 0 points
    And all teams should have 0 games played

  Scenario: Round robin standings count only completed games
    Given a round-robin phase with group "A" containing teams "Eagles", "Hawks", "Wolves"
    And "Eagles" beat "Hawks" 2-0
    And the game between "Eagles" and "Wolves" is still scheduled
    When I request standings for group "A"
    Then "Eagles" should have 1 game played
    And "Wolves" should have 0 games played

  # --- Playoff Elimination Standings ---

  Scenario: Elimination standings with 4 teams fully completed
    Given a playoff-elimination phase with group "A" containing 4 teams
    And all bracket games are completed
    When I request standings for group "A"
    Then the winner of the final should be position 1
    And the loser of the final should be position 2
    And the losers of the semifinals should both be position 3

  Scenario: Elimination standings with 8 teams
    Given a playoff-elimination phase with group "A" containing 8 teams
    And all quarterfinal games are completed
    When I request standings for group "A"
    Then all 4 QF losers should share position 5
    And the 4 QF winners should not have final positions yet

  Scenario: Elimination standings with partial bracket
    Given a playoff-elimination phase with group "A" containing 4 teams
    And only the first semifinal game is completed
    When I request standings for group "A"
    Then only the loser of that game should have a confirmed position (3)

  # --- Playoff with Placement Standings ---

  Scenario: Placement standings with 4 teams fully completed
    Given a playoff-with-placement phase with group "A" containing 4 teams
    And all games including placement games are completed
    When I request standings for group "A"
    Then each team should have a unique position from 1 to 4
    And the winner of the Final should be 1st
    And the loser of the Final should be 2nd
    And the winner of the 3rd-place game should be 3rd
    And the loser of the 3rd-place game should be 4th

  Scenario: Placement standings with 8 teams fully completed
    Given a playoff-with-placement phase with group "A" containing 8 teams
    And all games including placement games are completed
    When I request standings for group "A"
    Then each team should have a unique position from 1 to 8

  Scenario: Placement standings with incomplete placement games
    Given a playoff-with-placement phase with group "A" containing 4 teams
    And only the semifinals are completed (no placement games yet)
    When I request standings for group "A"
    Then no team should have a confirmed position yet

  # --- API Access ---

  Scenario: Anonymous user can view standings
    Given a round-robin phase with completed games
    When an anonymous user requests standings for a group
    Then the standings should be returned successfully

  Scenario: Standings for nonexistent tournament returns 404
    When I request standings for a nonexistent tournament
    Then I should receive a 404 Not Found response

  Scenario: Standings include team names
    Given a round-robin phase with group "A" and completed games
    When I request standings for group "A"
    Then each standing entry should include the team name
