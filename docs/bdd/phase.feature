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

  Scenario: Owner sets progression rules
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with phases "Groups" and "Playoffs"
    When the user sets progression rules from "Groups" to "Playoffs"
    Then the progression rules are stored correctly

  Scenario: Non-owner cannot add a phase
    Given user "Alice" owns tournament "Summer Cup" with a structure
    When user "Bob" attempts to add a phase
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor can view phases
    Given tournament "Summer Cup" has phases
    When a visitor requests the phases of "Summer Cup"
    Then the phase list is returned successfully
