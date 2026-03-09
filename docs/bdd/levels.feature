Feature: Phase Levels

  Levels allow a phase to be split into divisions (e.g., Gold/Silver) so that
  teams of similar strength compete against each other. When enabled, groups
  are evenly distributed across levels.

  Scenario: Creating a phase with levels generates correct groups and levels
    Given the user is authenticated
    And the user owns a tournament with a structure
    When the user adds a phase with 2 groups and 2 levels
    Then the phase has 4 groups total
    And the phase has 2 levels

  Scenario: Groups are linked to their respective levels
    Given the user is authenticated
    And the user owns a tournament with a structure
    When the user adds a phase with 2 groups and 2 levels
    Then each level has exactly 2 groups linked to it
    And every group has a LevelId assigned

  Scenario: Levels have default naming
    Given the user is authenticated
    And the user owns a tournament with a structure
    When the user adds a phase with 2 groups and 2 levels
    Then the levels are named "Level 1" and "Level 2"

  Scenario: Group naming is sequential across all levels
    Given the user is authenticated
    And the user owns a tournament with a structure
    When the user adds a phase with 2 groups and 2 levels
    Then the groups are named "A", "B", "C", "D" in order

  Scenario: Updating a level name
    Given the user is authenticated
    And the user owns a tournament with a phase that has 2 levels
    When the user updates the first level name to "Gold"
    Then the level name is "Gold"

  Scenario: Level names must be unique within a phase
    Given the user is authenticated
    And the user owns a tournament with a phase that has 2 levels
    When the user checks if "Level 1" already exists in the phase
    Then the duplicate check returns true

  Scenario: Creating a phase without levels (backward compatibility)
    Given the user is authenticated
    And the user owns a tournament with a structure
    When the user adds a phase with 2 groups and no levels
    Then the phase has 2 groups
    And the phase has no levels
    And no group has a LevelId

  Scenario: NumberOfGroups is per-level
    Given the user is authenticated
    And the user owns a tournament with a structure
    When the user adds a phase with 3 groups and 2 levels
    Then the phase has 6 groups total
    And each level has exactly 3 groups linked to it

  Scenario: Retrieving groups for a specific level
    Given the user is authenticated
    And the user owns a tournament with a phase that has 2 groups and 3 levels
    When the user retrieves groups for the first level
    Then only the 2 groups belonging to that level are returned
