Feature: Court Management

  Scenario: Owner adds, renames, and deletes a court
    Given the user owns a tournament
    When the user adds a court, renames it, and deletes it
    Then each change is reflected in the court list

  Scenario: Duplicate court name is rejected
    Given a tournament with an existing court
    When the user adds another court with the same name
    Then the request is rejected with status 400

  Scenario Outline: Court access control
    Given a tournament owned by Alice
    When <actor> attempts to <action>
    Then the request is <result>

    Examples:
      | actor               | action            | result                   |
      | Bob                 | add a court       | rejected with status 403 |
      | an anonymous visitor | view courts       | returned successfully    |
      | an anonymous visitor | add a court       | rejected with status 401 |

  # --- Generate Courts ---

  Scenario: Owner generates courts additively
    Given a tournament with some existing courts
    When the owner generates N additional courts
    Then N new courts are added named C{next}..C{next+N-1}

  Scenario Outline: Generate-courts validation and auth
    When <actor> attempts <action>
    Then the request is rejected with status <status>

    Examples:
      | actor                | action                          | status |
      | the owner            | generating 0 or 21 courts       | 400    |
      | a non-owner          | generating courts               | 403    |
      | an anonymous visitor | generating courts               | 401    |
