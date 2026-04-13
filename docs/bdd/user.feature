Feature: User Authentication and Roles

  # Permission tiers: Anonymous, User, Admin, Participant (code).

  Scenario Outline: Anonymous visitor access
    When an anonymous visitor <action>
    Then the request is <result>

    Examples:
      | action                           | result                    |
      | views the tournament list        | returned successfully     |
      | creates a tournament             | rejected with status 401  |
      | updates or deletes a tournament  | rejected with status 401  |

  Scenario: Authenticated user can create tournaments
    Given the user is authenticated
    When the user creates a tournament
    Then the tournament is created successfully

  Scenario Outline: Admin can manage any tournament and see protected fields
    Given user "Alice" owns a tournament
    And an admin is authenticated
    When the admin performs <action>
    Then the action succeeds and protected data is visible where applicable

    Examples:
      | action                                    |
      | update Alice's tournament                 |
      | delete Alice's tournament                 |
      | view the tournament list (codes visible)  |
      | view team contact info                    |
      | record a game result without the tournament code |

  Scenario: Login response includes the user's role
    Given a user exists with a role
    When the user logs in with valid credentials
    Then the login response includes that role
