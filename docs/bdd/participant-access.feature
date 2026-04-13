Feature: Participant Access via Tournament Code
  As a tournament participant
  I want to record game results using a tournament code
  So that I don't need to create an account

  Background:
    Given a tournament with a valid tournament code and a scheduled game

  Scenario Outline: Tournament-code authentication for recording results
    When I submit a result with <auth>
    Then the request is <result>

    Examples:
      | auth                                         | result                      |
      | the correct tournament code                  | accepted                    |
      | the correct code in a different case         | accepted (case-insensitive) |
      | an incorrect tournament code                 | rejected with status 403    |
      | no tournament code and no JWT                | rejected with status 401    |

  Scenario Outline: Record result in either mode
    Given a scheduled game and a valid tournament code
    When I submit <submission>
    Then the result is stored and the game is Completed

    Examples:
      | submission                           |
      | a detailed per-set score breakdown   |
      | a simple sets-won result             |

  Scenario Outline: Tournament code only grants result recording
    When I use the tournament code to <action>
    Then the request is rejected with status 401

    Examples:
      | action                |
      | create a team         |
      | add a phase           |

  Scenario: Authenticated owner records a result without a tournament code
    Given the owner is authenticated
    When the owner submits a result without providing a code
    Then the result is recorded

  Scenario: Non-owner authenticated user cannot record a result without the code
    Given another authenticated user is not the tournament owner
    When they submit a result without providing the tournament code
    Then the request is rejected with status 403
