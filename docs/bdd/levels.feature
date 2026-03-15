Feature: Phase Levels

  Levels allow a phase to be split into divisions (e.g., Gold/Silver) so that
  teams of similar strength compete against each other. When enabled, groups
  are evenly distributed across levels. Levels cascade forward: each subsequent
  phase must have at least as many levels as the previous phase (and the count
  must be a multiple of the previous phase's level count).

  # Basic level creation and structure

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

  # Cascading level constraint

  Scenario: Phase 2 can have the same number of levels as Phase 1
    Given a structure with Phase 1 having 2 levels
    When the user adds Phase 2 with 2 levels
    Then the phase is created successfully

  Scenario: Phase 2 can have a multiple of Phase 1's levels
    Given a structure with Phase 1 having 2 levels
    When the user adds Phase 2 with 4 levels
    Then the phase is created successfully

  Scenario: Phase 2 cannot have fewer levels than Phase 1
    Given a structure with Phase 1 having 2 levels
    When the user adds Phase 2 with 1 level
    Then a 400 error is returned with message about invalid level count

  Scenario: Phase 2 levels must be a multiple of Phase 1 levels
    Given a structure with Phase 1 having 2 levels
    When the user adds Phase 2 with 3 levels
    Then a 400 error is returned with message about invalid level count

  Scenario: Phase without levels can be followed by phase with levels
    Given a structure with Phase 1 having no levels
    When the user adds Phase 2 with 2 levels
    Then the phase is created successfully

  Scenario: Phase without levels can be followed by phase without levels
    Given a structure with Phase 1 having no levels
    When the user adds Phase 2 with no levels
    Then the phase is created successfully

  Scenario: Phase with levels cannot be followed by phase without levels
    Given a structure with Phase 1 having 2 levels
    When the user adds Phase 2 with no levels
    Then a 400 error is returned with message about invalid level count

  # Auto-assign

  Scenario: Auto-assign with levels splits teams by seed into levels
    Given 8 teams ranked 1-8 by level
    And a phase with 2 levels and 2 groups per level (4 groups total)
    When I auto-assign teams to the phase
    Then Level 1 groups contain teams ranked 1-4
    And Level 2 groups contain teams ranked 5-8
    And teams within each level are snake-drafted across that level's groups

  Scenario: Auto-assign phase 2+ with levels distributes placeholders by level
    Given 8 placeholder teams ordered by seed 1-8
    And the target phase has 2 levels and 2 groups per level
    When I auto-assign placeholders to the phase
    Then Level 1 groups contain seeds 1-4
    And Level 2 groups contain seeds 5-8

  Scenario: Auto-assign without levels is unchanged
    Given 4 teams ranked 1-4 by level
    And a phase with 2 groups and no levels
    When I auto-assign teams to the phase
    Then teams are snake-drafted across all groups as before

  # Progression with levels (same level count)

  Scenario: Phase advancement with levels qualifies per-level
    Given a phase with 2 levels, 2 groups per level, TotalTeamsProceeding=3
    When the phase is completed
    Then 3 teams qualify from Level 1 and 3 from Level 2 (6 total)
    And Level 1 qualifiers are seeded before Level 2 qualifiers

  # Level-scoped progression (cross-phase level increase)

  Scenario: Progression with level split distributes teams to child levels
    Given Phase 1 with 2 levels, 2 groups per level, TotalTeamsProceeding=4
    And Phase 2 with 4 levels, 1 group per level
    When Phase 1 is completed
    Then Phase 1 Level 1's 4 qualifying teams feed into Phase 2 Levels 1-2
    And Phase 1 Level 2's 4 qualifying teams feed into Phase 2 Levels 3-4

  Scenario: Progression from no-levels to levels treats all teams as single pool
    Given Phase 1 with no levels, 2 groups, TotalTeamsProceeding=4
    And Phase 2 with 2 levels, 1 group per level
    When Phase 1 is completed
    Then all 4 qualifying teams are distributed across Phase 2 levels by seed
    And top 2 seeds go to Level 1 and bottom 2 seeds go to Level 2

  Scenario: Progression with same level count works as before
    Given Phase 1 with 2 levels, 2 groups per level, TotalTeamsProceeding=3
    And Phase 2 with 2 levels, 1 group per level
    When Phase 1 is completed
    Then Phase 1 Level 1 qualifiers seed into Phase 2 Level 1
    And Phase 1 Level 2 qualifiers seed into Phase 2 Level 2

  # Placeholder generation

  Scenario: Placeholder count with levels scales by level count
    Given a phase with TotalTeamsProceeding=4 and 2 levels
    When I create the next phase with the same number of levels
    Then 8 placeholder teams are generated (4 per level x 2 levels)

  Scenario: Placeholder generation with level split distributes across target levels
    Given Phase 1 with 2 levels, TotalTeamsProceeding=4
    And Phase 2 is created with 4 levels
    Then 8 placeholders are generated total
    And Phase 1 Level 1's 4 placeholders are distributed across Phase 2 Levels 1-2
    And Phase 1 Level 2's 4 placeholders are distributed across Phase 2 Levels 3-4

  Scenario: Placeholder generation from no-levels to levels
    Given Phase 1 with no levels, TotalTeamsProceeding=4
    And Phase 2 is created with 2 levels
    Then 4 placeholders are generated
    And placeholders are distributed across Phase 2 levels by seed
