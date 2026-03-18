Feature: Double Elimination (VD) Phase Format
  As a tournament organizer
  I want to use a Double Elimination (VD) bracket format
  So that I can run a volleyball-specific double elimination for exactly 8 teams

  Background:
    Given a tournament with 8 teams and a phase with DoubleEliminationVd format

  Scenario: Generate bracket for 8 teams
    When I generate games for the phase
    Then 14 games should be generated across 7 rounds
    And round 1 has 4 quarter-final games with real team IDs
    And round 2 has 2 QF winners games with placeholders
    And round 3 has 2 QF losers games with placeholders
    And round 4 has 2 crossover games with cross-bracket placeholders
    And round 5 has 2 grand semi-final games
    And round 6 has 1 seventh place game
    And round 7 has 1 grand final game

  Scenario: QF seeding follows standard bracket order
    When I generate games for the phase
    Then QF1 is seed 1 vs seed 8
    And QF2 is seed 4 vs seed 5
    And QF3 is seed 2 vs seed 7
    And QF4 is seed 3 vs seed 6

  Scenario: Crossover uses cross-bracket pairings
    When I generate games for the phase
    Then X1 pairs Loser W2 vs Winner L1
    And X2 pairs Loser W1 vs Winner L2

  Scenario: Grand SFs use same-half pairings
    When I generate games for the phase
    Then GSF1 pairs Winner W1 vs Winner X1
    And GSF2 pairs Winner W2 vs Winner X2

  Scenario: Reject non-8 team groups
    Given a group with 6 teams
    When I try to generate games for the phase
    Then I get a validation error about requiring exactly 8 teams

  Scenario: Standings after full completion
    Given all 14 games have been played
    When I view standings
    Then the Grand Final winner is 1st
    And the Grand Final loser is 2nd
    And both Grand SF losers share 3rd
    And both Crossover losers share 5th
    And the 7th Place winner is 7th
    And the 7th Place loser is 8th

  Scenario: Bracket advancement resolves placeholders
    Given QF1 result is recorded with team 1 winning
    When I view games in the bracket
    Then W1 home team is resolved to team 1
    And L1 home team is resolved to team 1's opponent
