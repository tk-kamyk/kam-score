Feature: Game Generation and Results
  As a tournament owner
  I want to generate and schedule games and record results
  So that teams can compete according to the tournament format

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with courts, a game length, and a structure with phases, groups, and assigned teams

  # --- Generation (one happy-path per format; per-format bracket math is unit-tested) ---

  Scenario Outline: Owner generates and schedules games for a phase
    Given a <format> phase with a start time
    When I generate and schedule games for the phase
    Then games are created for each group
    And all games have a court and start time assigned

    Examples:
      | format                  |
      | round-robin             |
      | playoff-elimination     |
      | playoff-with-placement  |
      | double-elimination      |
      | double-elimination-vd   |

  Scenario: Owner cannot generate games twice for the same phase
    Given games have already been generated for a phase
    When I try to generate and schedule games for the phase
    Then the request is rejected

  Scenario Outline: Generation is rejected when prerequisites are missing
    Given the tournament is missing <prerequisite>
    When I try to generate and schedule games for a phase
    Then the request is rejected

    Examples:
      | prerequisite        |
      | courts              |
      | phase start time    |
      | tournament game length |

  # --- Retrieval ---

  Scenario: Anyone can view generated games with full details
    Given a phase with generated and scheduled games
    When anyone requests the games for the tournament
    Then the response includes team names, court names, times, and status

  Scenario: Games can be filtered by court
    Given a phase with generated and scheduled games
    When I request games filtered by a court
    Then only games scheduled on that court are returned

  # --- Deletion ---

  Scenario: Owner can delete all games for a phase
    Given a phase with generated games
    When the owner deletes games for the phase
    Then all games for that phase are removed
    And the owner can generate new games for the phase

  Scenario: Non-owner cannot generate or delete games
    Given a phase in someone else's tournament
    When I try to generate or delete games
    Then the request is rejected with status 403

  # --- Recording results ---

  Scenario: Participant records a detailed result using tournament code
    Given a scheduled game in a phase with generated games
    When a participant submits per-set scores using a valid tournament code
    Then the game status becomes Completed
    And the per-set breakdown and aggregate score are stored

  Scenario: Participant records a simple result (sets won)
    Given a scheduled game in a phase with generated games
    When a participant submits a sets-won result using a valid tournament code
    Then the game status becomes Completed

  Scenario: Owner can edit an already-recorded result
    Given a game with a recorded result
    When the owner submits a new result for the same game
    Then the game remains Completed with the new result

  Scenario Outline: Tie rules are enforced
    Given a scheduled game in a phase with generated games
    When a participant submits <submission>
    Then the request is rejected with status 400

    Examples:
      | submission                                              |
      | a simple result that is a tie                           |
      | a multi-set detailed result that is a tie in set count  |
      | a multi-set detailed result containing a drawn set      |

  Scenario: A single-set detailed result may be a tie
    Given a scheduled game in a phase with generated games
    When a participant submits a single-set detailed result with equal points
    Then the game status becomes Completed

  # --- Bracket advancement (behavioral; unit tests cover the resolution algorithm) ---

  Scenario: Recording a playoff result advances winner and loser to downstream games
    Given a playoff phase with generated games where downstream games reference earlier rounds via placeholders
    When a result is recorded for an early-round game
    Then downstream games referencing the winner show the winning team
    And downstream games referencing the loser show the losing team

  Scenario: A tied playoff result does not trigger advancement
    Given a playoff phase with generated games
    When an early-round playoff game is recorded as a tie
    Then downstream games remain on their placeholders

  Scenario: Correcting a playoff result re-resolves downstream teams
    Given a playoff phase with an already-recorded early-round result
    When the result is corrected in favour of the other team
    Then downstream games are updated to reflect the new winner and loser

  Scenario: Round-robin results do not trigger bracket advancement
    Given a round-robin phase with generated games
    When a result is recorded
    Then no other games are modified
