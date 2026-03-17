Feature: Phase Management

  Scenario: Owner adds a phase to the structure
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds a phase with:
      | Name   | Group Stage  |
      | Format | RoundRobin   |
    Then the phase is added with order 1
    And the phase has the correct format

  Scenario: Adding multiple phases assigns sequential order
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds phases "Group Stage" and "Playoffs"
    Then "Group Stage" has order 1
    And "Playoffs" has order 2

  Scenario: Owner updates a phase
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phase "Group Stage"
    When the user updates the phase name to "Pool Stage"
    Then the phase name is "Pool Stage"

  Scenario: Owner deletes a phase
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phase "Group Stage"
    When the user deletes the phase
    Then the structure has no phases

  Scenario: Deleting a phase reorders remaining phases
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phases "Groups", "Semis", "Finals"
    When the user deletes phase "Semis"
    Then "Groups" has order 1
    And "Finals" has order 2

  Scenario: Owner adds groups to a phase
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phase "Group Stage"
    When the user adds groups "A", "B", "C" to the phase
    Then the phase has 3 groups

  Scenario: Owner creates a phase with number of groups
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds a phase with:
      | Name            | Group Stage  |
      | Format          | RoundRobin   |
      | NumberOfGroups  | 4            |
    Then the phase is created with 4 groups named "A", "B", "C", "D"

  Scenario: Owner creates a phase with progression fields
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds a phase with:
      | Name                 | Group Stage  |
      | Format               | RoundRobin   |
      | GroupWinners         | 2            |
      | TotalTeamsProceeding | 6            |
    Then the phase has groupWinners 2
    And the phase has totalTeamsProceeding 6

  Scenario: Progression fields are optional
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds a phase without progression fields
    Then the phase has no groupWinners
    And the phase has no totalTeamsProceeding

  Scenario: Owner updates progression fields
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phase "Group Stage"
    When the user updates the phase with:
      | GroupWinners         | 1 |
      | TotalTeamsProceeding | 4 |
    Then the phase has groupWinners 1
    And the phase has totalTeamsProceeding 4

  Scenario: Owner creates a phase with start time
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds a phase with:
      | Name      | Group Stage |
      | Format    | RoundRobin  |
      | StartTime | 09:30       |
    Then the phase has startTime "09:30"

  Scenario: Owner updates phase start time
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phase "Group Stage"
    When the user updates the phase with:
      | StartTime | 14:00 |
    Then the phase has startTime "14:00"

  Scenario: Owner creates a phase with zero progression (final phase)
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user adds a phase with:
      | Name                 | Final Round  |
      | Format               | RoundRobin   |
      | GroupWinners         | 0            |
      | TotalTeamsProceeding | 0            |
    Then the phase has groupWinners 0
    And the phase has totalTeamsProceeding 0

  Scenario: Non-owner cannot add a phase
    Given user "Alice" owns tournament "Summer Cup" with a structure
    When user "Bob" attempts to add a phase
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor can view phases
    Given tournament "Summer Cup" has phases
    When a visitor requests the phases of "Summer Cup"
    Then the phase list is returned successfully
