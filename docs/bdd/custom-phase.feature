Feature: Custom Phase Format (Manual Standings)
  As a tournament organizer whose matches happen outside the system
  I want to use a Custom phase format and enter standings manually per group
  So that progression, seeding, and final standings still work end-to-end

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with a structure

  # Phase creation & editing

  Scenario: Information message is shown when selecting Custom
    Given I am editing a phase
    When I select format "Custom"
    Then an information message explains that no games will be created and standings will be entered manually

  Scenario: Owner creates a Custom phase
    When I add a phase with format "Custom", 2 groups, and teams assigned
    Then the phase is stored with format Custom and status New
    And no games are created for the phase

  Scenario: Changing phase format away from Custom clears manual standings
    Given a Custom phase with manual standings saved for every group
    When I change the phase's format to "RoundRobin"
    Then all manual standings for the phase are cleared

  # Starting a Custom phase (reuses the "Generate games" action)

  Scenario: Starting a Custom phase activates it without generating games
    Given a Custom phase with at least one team assigned per group
    When I trigger the phase start action
    Then the phase transitions to InProgress
    And no games are created

  Scenario: Starting a Custom phase requires every group to have at least one team
    Given a Custom phase with one group containing no teams
    When I trigger the phase start action
    Then the request is rejected with a validation error

  Scenario: Custom phases skip the generic scheduling prerequisites
    Given the tournament has no game length, no phase start time, and no courts configured
    And a Custom phase with teams assigned in every group
    When I trigger the phase start action
    Then the phase transitions to InProgress successfully

  # Manual standings entry (validation rules unit-tested; representative API scenarios only)

  Scenario: Owner saves a complete manual order for a group
    Given a Custom phase in InProgress with a group of 3 teams
    When I save the group's standings ordered [team-3, team-1, team-2]
    Then the returned standings list team-3 at position 1, team-1 at position 2, team-2 at position 3
    And per-team stats (wins, points, set difference) are blank

  Scenario Outline: Invalid manual standings are rejected
    Given a Custom phase in InProgress with a group of 3 teams
    When I save the group's standings with <invalid ordering>
    Then the request is rejected with status 400

    Examples:
      | invalid ordering                             |
      | a team ID not in the group                    |
      | a duplicated team ID                          |
      | fewer team IDs than the group has teams       |
      | more team IDs than the group has teams        |

  Scenario: Manual standings cannot be saved when the phase is not InProgress
    Given a Custom phase in status New
    When I save a group's standings
    Then the request is rejected

  Scenario: Removing a team from a Custom group clears that group's manual standings
    Given a Custom phase in InProgress with a group of 3 teams and a saved ordering
    When I remove a team from the group
    Then the group's manual standings are cleared

  # Phase completion

  Scenario: Completing a Custom phase requires a complete order in every group
    Given a Custom phase in InProgress where one group has no manual standings saved
    When I mark the phase as complete
    Then the request is rejected with a validation error

  Scenario: Completing a Custom phase resolves placeholders in the next phase using manual positions
    Given a Custom phase with progression config and complete manual standings in every group
    And a next phase with placeholder teams
    When I mark the Custom phase as complete
    Then the next phase's games and groups contain real team IDs seeded from the manual positions

  Scenario: Reopening a completed Custom phase re-enables editing and reverses placeholders
    Given a completed Custom phase with a next phase in New with resolved placeholders
    When I reopen the Custom phase
    Then the phase is InProgress and the next phase reverts to New with placeholder IDs restored
    And I can save a new ordering for any group

  # Public surfaces

  Scenario: Anonymous user can view standings for a Custom phase
    Given a Custom phase in InProgress with manual standings saved in a group
    When an anonymous user requests standings for the group
    Then the standings are returned with positions derived from the manual order

  Scenario: Final standings reflect the manual order when the last phase is Custom
    Given a tournament whose last phase is a completed Custom phase
    When an anonymous user requests final standings
    Then the standings are returned in the manually entered order

  # Access control

  Scenario Outline: Only the tournament owner can save manual standings
    Given a Custom phase in InProgress in someone else's tournament
    When <actor> attempts to save a group's standings
    Then the request is rejected with status <status>

    Examples:
      | actor                   | status |
      | another authenticated user | 403  |
      | an anonymous visitor       | 401  |
      | a participant with a tournament code | 403 |
