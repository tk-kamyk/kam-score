Feature: Phase Management

  # Phase-state restrictions live in phase-state-restrictions.feature.
  # Progression & placeholders live in progression.feature.
  # Levels live in levels.feature.

  Scenario: Owner creates phases and they are sequentially ordered
    Given the user owns a tournament with a structure
    When the user adds two phases in order
    Then each phase receives a sequential order starting at 1

  Scenario: Deleting a phase reorders remaining phases
    Given a structure with three phases
    When the middle phase is deleted
    Then the remaining phases keep a contiguous order

  Scenario: Owner creates a phase with configurable properties
    Given the user owns a tournament
    When the user adds a phase with name, format, number of groups, start time, and progression fields
    Then the phase stores all of those values
    And groups are auto-named A, B, C…

  Scenario: Progression fields are optional
    Given the user owns a tournament
    When the user adds a phase without progression fields
    Then the phase has no groupWinners and no totalTeamsProceeding

  Scenario: Owner updates a phase's editable fields
    Given a phase exists
    When the user updates its name, progression fields, or start time
    Then the updates are stored

  Scenario: Owner can mark a phase as final via zero progression
    Given the user owns a tournament
    When the user adds a phase with GroupWinners=0 and TotalTeamsProceeding=0
    Then the phase stores both as 0 and is treated as final

  Scenario Outline: Phase access control
    Given a tournament owned by Alice
    When <actor> attempts to <action>
    Then the request is <result>

    Examples:
      | actor               | action        | result                   |
      | Bob                 | add a phase   | rejected with status 403 |
      | an anonymous visitor | view phases   | returned successfully    |
