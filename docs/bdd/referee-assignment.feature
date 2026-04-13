Feature: Manual Referee Assignment

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with courts, teams, groups, and scheduled games

  # --- Candidate list ---
  # The referee eligibility algorithm is unit-tested (RefereeAssignerTests).

  Scenario: Candidate list is scoped by level when phase has levels
    Given a phase with multiple levels
    When I request candidates for a game in one level
    Then only teams from that level are returned

  Scenario: Candidate list spans all phase teams when phase has no levels
    Given a phase with no levels
    When I request candidates for a game
    Then teams from all groups in the phase are returned

  Scenario Outline: Teams busy in or around the time slot are excluded
    Given a game at 10:00 with no referee
    And another team <activity>
    When I request candidates for the 10:00 game
    Then that team is not in the candidate list

    Examples:
      | activity                                     |
      | plays in the 10:00 slot                      |
      | referees in the 10:00 slot                   |
      | plays in the next slot (10:30)               |
      | is the home or away team of the target game |

  # --- Elimination bracket placeholders ---

  Scenario: Candidate list includes unresolved bracket placeholders from earlier rounds
    Given a later-round elimination game with no referee
    When I request candidates for that game
    Then placeholders from earlier rounds are included and marked as such

  Scenario Outline: Placeholders busy in or around the time slot are excluded
    Given a placeholder <activity>
    When I request candidates for the target game
    Then that placeholder is not in the candidate list

    Examples:
      | activity                                     |
      | is the home or away of the target game       |
      | plays in the same slot in another game       |
      | plays in the next slot                       |

  # --- Assignment & resolution ---

  Scenario: Owner assigns a real team as referee
    Given a game with no referee and a valid candidate team
    When the owner assigns the team as referee
    Then the game shows that team as its referee

  Scenario: Owner assigns a placeholder as referee
    Given a game with no referee and a valid placeholder candidate
    When the owner assigns the placeholder as referee
    Then the game shows the placeholder label as referee (no team ID until resolved)

  Scenario: Re-assigning a referee replaces the current assignment
    Given a game with an existing referee
    When the owner assigns a different valid candidate
    Then the game shows the new referee and the previous one is cleared

  Scenario: Referee placeholder resolves when the referenced game completes
    Given a game with a "Loser {label}" referee placeholder
    When the referenced upstream game's result is recorded
    Then the game's refereeTeamId is set while the placeholder label is preserved

  Scenario: Ineligible team assignment is rejected
    Given a game with no referee
    When the owner tries to assign a team busy in the same slot
    Then the request is rejected with status 400

  # --- Access control ---

  Scenario Outline: Only the owner can assign or view candidates
    Given a game in someone else's tournament
    When I try to <action>
    Then the request is rejected with status 403

    Examples:
      | action                  |
      | assign a referee        |
      | view referee candidates |
