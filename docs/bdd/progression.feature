Feature: Phase Advancement
  As a tournament organizer
  I want teams to automatically advance between phases based on standings
  So that the tournament flows from group stage to playoffs seamlessly

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with a structure

  # Phase status transitions (detailed state matrix lives in
  # phase-state-restrictions.feature; seeding math is unit-tested).

  Scenario Outline: Phase status transitions
    Given <precondition>
    When <action>
    Then the phase status becomes "<status>"

    Examples:
      | precondition                                             | action                                         | status     |
      | a newly added phase                                      | I inspect it                                   | New        |
      | the first phase with teams assigned                      | I generate and schedule games                  | InProgress |
      | a phase with all games completed                         | I mark it as complete                          | Completed  |
      | a completed phase followed by a next phase with placeholder games | I mark the first phase complete       | Completed  |

  Scenario: Cannot complete a phase with unfinished games
    Given a phase in InProgress with some games still scheduled
    When I try to mark the phase as complete
    Then the request is rejected

  # Progression configuration

  Scenario Outline: Progression config controls how many teams advance
    Given a completed phase with 2 groups of 4 teams and <config>
    When the phase is completed
    Then <outcome>

    Examples:
      | config                                         | outcome                                                   |
      | GroupWinners=2 and TotalTeamsProceeding=4      | top 2 from each group qualify                             |
      | GroupWinners=1 and TotalTeamsProceeding=4      | 1 winner per group plus 2 best-remaining teams qualify    |
      | only TotalTeamsProceeding=4                    | top 4 teams across all groups qualify                     |
      | only GroupWinners=2                            | top 2 from each group qualify (4 total)                   |
      | neither set                                    | no teams advance                                          |
      | GroupWinners=0 and TotalTeamsProceeding=0      | no teams advance but the phase is treated as final        |

  Scenario: Group winners are seeded above runners-up when both progression flags are set
    Given a completed round-robin phase with 8 groups of 3 teams
    And the phase has GroupWinners=1 and TotalTeamsProceeding=24
    And one group winner has worse overall stats than several runners-up in other groups
    When the phase is completed
    Then all 8 group winners receive seeds 1 through 8
    And no runner-up is seeded above any group winner

  Scenario: Best-remaining ranking is position-major when both progression flags are set
    Given a completed round-robin phase with 2 groups
    And the phase has GroupWinners=1 and TotalTeamsProceeding=4
    And in one group a 2nd-place runner-up has worse stats than a 3rd-place team in the other group
    When the phase is completed
    Then both group winners qualify
    And both 2nd-place teams qualify ahead of any 3rd-place team regardless of stats

  # Placeholder lifecycle

  Scenario: Placeholder teams are created when a successor phase is added
    Given a phase with progression config and a known qualifying count
    When a successor phase is added
    Then placeholder teams are created matching the qualifying count
    And each placeholder references the source phase and has a unique seed

  Scenario: Placeholder teams are regenerated when progression config changes
    Given a phase with placeholders in a successor phase
    When the source phase's progression config is updated
    Then old placeholders are deleted, new placeholders are created, and any games in the successor phase are deleted

  Scenario: Placeholder teams are deleted when their source phase is deleted
    Given a successor phase with placeholders from a source phase
    When the source phase is deleted
    Then the associated placeholder teams are deleted

  Scenario: Deleting a middle phase regenerates placeholders for the successor
    Given three consecutive phases each with progression config
    When the middle phase is deleted
    Then placeholders in the last phase are regenerated from the now-previous phase
    And existing games and group assignments in the last phase are cleared

  # Placeholder resolution

  Scenario: Completing a phase resolves placeholder IDs to real team IDs
    Given a completed phase with progression and a next phase using placeholder team IDs in games and groups
    When the phase is marked complete
    Then the next phase's games and groups contain real team IDs
    And each placeholder team records the resolved real team ID

  Scenario: Reopening a completed phase reverses placeholder resolution
    Given a completed phase with a next phase that has resolved placeholders
    When the first phase is reopened
    Then the next phase reverts to New and the placeholder IDs are restored in games and groups

  # Access control

  Scenario Outline: Only owner can transition phase status
    Given a phase in someone else's tournament
    When I try to <action>
    Then the request is rejected with status 403

    Examples:
      | action          |
      | complete a phase |
      | reopen a phase   |
