Feature: Phase Levels

  Levels split a phase into divisions (e.g., Gold/Silver) so teams of similar
  strength compete. When enabled, groups are distributed across levels and the
  level count must cascade forward as a multiple through later phases.

  # Basic level creation — formulas and group-count math live in unit tests.

  Scenario: Adding a phase with levels creates the levels and distributes groups
    Given I am an authenticated tournament owner
    When I add a phase with N groups and L levels
    Then the phase has N×L total groups distributed evenly across L levels
    And each group has a LevelId assigned
    And levels are given default names ("Level 1", …) which can be edited

  Scenario: Adding a phase without levels keeps the simple group layout
    Given I am an authenticated tournament owner
    When I add a phase with N groups and no levels
    Then the phase has N groups with no LevelId

  Scenario: Level names must be unique within a phase
    Given a phase with levels
    When two levels are given the same name
    Then the duplicate is rejected

  # Cascading level constraint

  Scenario Outline: Phase N+1's level count must be a multiple of Phase N's
    Given Phase 1 has <phase1Levels> levels
    When I add Phase 2 with <phase2Levels> levels
    Then the request <result>

    Examples:
      | phase1Levels | phase2Levels | result                         |
      | 2            | 2            | succeeds                       |
      | 2            | 4            | succeeds                       |
      | 2            | 1            | is rejected with status 400    |
      | 2            | 3            | is rejected with status 400    |
      | none         | 2            | succeeds                       |
      | none         | none         | succeeds                       |
      | 2            | none         | is rejected with status 400    |

  # Auto-assign and progression — split-factor math is unit-tested.

  Scenario: Auto-assign with levels splits teams by seed into levels
    Given a phase with levels and groups per level
    When auto-assign runs
    Then teams are split top-half / bottom-half by seed across levels and snake-drafted within each level

  Scenario: Progression with levels qualifies per level
    Given a phase with levels and progression config
    When the phase is completed
    Then progression config is applied per level, with Level 1 qualifiers ranked above Level 2

  Scenario Outline: Level-scoped progression handles level changes across phases
    Given Phase 1 with <phase1Levels> and Phase 2 with <phase2Levels>
    When Phase 1 is completed
    Then qualifying teams are distributed according to the level-scoped split

    Examples:
      | phase1Levels | phase2Levels |
      | 2 levels     | 4 levels     |
      | no levels    | 2 levels     |
      | 2 levels     | 2 levels     |
