Feature: Group Standings
  As a tournament viewer
  I want to see standings for each group
  So that I can track team performance and positions

  Background:
    Given a tournament with a structure, phases, groups, and assigned teams

  # The tiebreaker cascade and per-format position formulas are exercised in
  # unit tests (StandingsCalculatorTests, per-format strategy tests). These
  # behavioural scenarios only check the public API shape and access rules.

  @FR-RES-020 @FR-RES-030
  Scenario: Owner sees standings for a group with completed games
    Given a phase with a group and at least one completed game
    When the owner requests standings for the group
    Then the standings are returned with positions, team names, and points/set/point stats

  @FR-RES-020
  Scenario: Standings reflect only completed games
    Given a phase with a mix of completed and scheduled games
    When standings are requested for a group
    Then only completed games contribute to each team's totals

  @FR-USR-001
  Scenario: Anonymous user can view standings
    Given a phase with completed games
    When an anonymous user requests standings for a group
    Then the standings are returned successfully

  @FR-RES-020
  Scenario: Standings for a nonexistent tournament returns 404
    When standings are requested for a nonexistent tournament
    Then the request is rejected with status 404

  # Progression highlighting — one representative scenario; math is unit-tested.

  @FR-RES-080 @FR-RES-081 @FR-RES-082 @FR-RES-083 @FR-RES-084 @FR-RES-085
  Scenario Outline: Progression highlighting reflects progression config
    Given a phase with progression config <config>
    When the owner views standings for any group
    Then direct qualifiers and candidates are highlighted according to the config

    Examples:
      | config                                   |
      | groupWinners only                        |
      | totalTeamsProceeding only                |
      | both groupWinners and totalTeamsProceeding |
      | neither (no highlighting)                |
