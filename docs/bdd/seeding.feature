Feature: Team Auto-Assignment
  As a tournament owner
  I want to auto-assign teams into groups
  So that groups are balanced based on team level

  Background:
    Given I am logged in as the tournament owner
    And a tournament exists with teams and a structure with groups

  @FR-STR-012
  Scenario: Auto-assign distributes teams via snake draft by level on the first phase
    Given teams with varying levels and a first phase with multiple groups
    When I auto-assign teams
    Then teams are distributed across groups following a snake draft ordered by level

  @FR-STR-012
  Scenario: Auto-assign on later phases uses non-level-based order
    Given a second phase with multiple groups
    When I auto-assign teams
    Then all teams are distributed across the groups without being ordered by level

  @FR-STR-013
  Scenario: Auto-assign replaces previous assignments
    Given teams are already assigned to groups
    When I auto-assign teams
    Then the previous assignments are replaced

  @FR-STR-012
  Scenario: Cannot auto-assign with no groups
    Given the phase has no groups
    When I try to auto-assign teams
    Then the request is rejected

  @FR-USR-011
  Scenario: Non-owner cannot auto-assign teams
    Given another user owns the tournament
    When I attempt to auto-assign teams
    Then the request is rejected with 403 Forbidden

  # TBC
  # Top-together seeding strategy is not yet implemented — sequential fill.
