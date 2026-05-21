Feature: Final Standings (Last Phase)
  As a tournament viewer
  I want to see the standings of the last phase of the tournament
  So that I can see where each team finished

  Background:
    Given a tournament with a structure, phases, groups, and assigned teams

  @FR-RES-110 @FR-RES-112 @FR-RES-113 @FR-RES-115
  Scenario Outline: Final standings reflect only the last completed phase
    Given a tournament where <configuration>
    When I request the final standings
    Then I see <outcome>

    Examples:
      | configuration                                                        | outcome                                                  |
      | a single completed phase with one group                              | the group's standings as positions 1..N                  |
      | a single completed phase with multiple groups                        | all teams ranked across groups                           |
      | multiple phases are completed and the last phase has fewer teams     | only the teams in the last phase ranked 1..N             |
      | the last phase has levels                                            | per-level standings with positions restarting at 1       |
      | the last phase has real and placeholder teams                        | only the real teams in the standings                     |

  @FR-RES-111
  Scenario Outline: Final standings are empty when not ready
    Given <state>
    When I request the final standings
    Then an empty list is returned

    Examples:
      | state                                                         |
      | no phases exist                                               |
      | the last phase is not yet completed                           |
      | the last phase is completed but no games have results         |

  @FR-RES-114
  Scenario: Anonymous user can view final standings
    Given a tournament with the last phase completed
    When an anonymous user requests the final standings
    Then the standings are returned successfully

  @FR-RES-114
  Scenario: Final standings for a nonexistent tournament returns 404
    When I request final standings for a nonexistent tournament
    Then the request is rejected with status 404
