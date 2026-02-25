Feature: Tournament Structure Management

  Scenario: Owner initializes tournament structure
    Given the user is authenticated
    And the user owns tournament "Summer Cup" without a structure
    When the user initializes the structure
    Then the tournament has an empty structure with no phases

  Scenario: Owner cannot initialize structure twice
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with a structure
    When the user attempts to initialize the structure again
    Then the request is rejected with 400 Bad Request

  Scenario: Anonymous visitor can view the structure
    Given tournament "Summer Cup" has a structure with phases
    When a visitor requests the structure of "Summer Cup"
    Then the structure is returned successfully

  Scenario: Non-owner cannot initialize structure
    Given user "Alice" owns tournament "Summer Cup"
    When user "Bob" attempts to initialize the structure
    Then the request is rejected with 403 Forbidden
