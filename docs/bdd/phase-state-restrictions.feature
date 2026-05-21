Feature: Phase State Restrictions
  As a tournament owner
  I want the system to prevent invalid operations on phases
  So that tournament data integrity is maintained

  Background:
    Given I am authenticated as the tournament owner
    And a tournament with an initialized structure exists

  # Matrix of restrictions by phase state and operation.
  # All blocked operations return HTTP 409 unless stated otherwise.
  @FR-PSR-001 @FR-PSR-002 @FR-PSR-003 @FR-PSR-004 @FR-PSR-005
  Scenario Outline: Operations blocked on completed phases
    Given a phase in state "Completed"
    When I <operation>
    Then the request is rejected with status 409

    Examples:
      | operation                    |
      | update the phase name        |
      | delete the phase             |
      | add a group                  |
      | rename a group               |
      | assign a team to a group     |
      | record a game result         |
      | delete the phase games       |

  @FR-PSR-010 @FR-PSR-011 @FR-PSR-012 @FR-PSR-013
  Scenario Outline: Structural changes blocked when games exist
    Given a phase with generated games
    When I <operation>
    Then the request is rejected with status 409

    Examples:
      | operation                    |
      | change the phase format      |
      | change the phase start time  |
      | delete the phase             |
      | add a group                  |
      | delete a group               |
      | assign a team to a group     |
      | remove a team from a group   |
      | auto-assign teams            |

  @FR-PSR-014 @FR-PSR-020
  Scenario Outline: Allowed changes on a phase with games
    Given a phase with generated games
    When I <operation>
    Then the request succeeds

    Examples:
      | operation                                           |
      | update the phase name                               |
      | update progression fields (groupWinners, total)     |
      | rename a group                                      |
      | delete the phase games (phase resets to New)        |

  @FR-PSR-021
  Scenario: Cannot reopen a phase when next phase has completed games
    Given a completed phase followed by a next phase with recorded results
    When I try to reopen the first phase
    Then the request is rejected with status 409

  @FR-PSR-022
  Scenario: Cannot record a result when teams are unassigned
    Given a playoff game with unresolved team placeholders
    When I try to record a result for the game
    Then the request is rejected with status 400

  @FR-PSR-040 @FR-PSR-041 @FR-PSR-042
  Scenario Outline: Referential integrity blocks deletion
    Given <dependency>
    When I try to delete the <entity>
    Then the request is rejected with status 409

    Examples:
      | dependency                                    | entity |
      | a team assigned to a group                    | team   |
      | a team referenced in scheduled games          | team   |
      | a court with scheduled games                  | court  |
