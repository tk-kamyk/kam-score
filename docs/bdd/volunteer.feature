Feature: Volunteer Management

  # --- CRUD ---

  Scenario: Owner adds a volunteer to a tournament
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with team "Eagles"
    When the user adds a volunteer with:
      | Name    | John Doe       |
      | Contact | john@email.com |
      | TeamId  | <Eagles ID>    |
    Then the volunteer is added to the tournament
    And the volunteer's team is "Eagles"

  Scenario: Owner adds a volunteer with only a name
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user adds a volunteer with:
      | Name | Jane Doe |
    Then the volunteer is added to the tournament
    And the volunteer's contact is empty
    And the volunteer's team is empty

  Scenario: Owner updates a volunteer
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with volunteer "John Doe"
    When the user updates volunteer "John Doe" name to "John Smith" and contact to "john@new.com"
    Then the volunteer name is "John Smith"
    And the volunteer contact is "john@new.com"

  Scenario: Owner deletes a volunteer
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with volunteer "John Doe"
    When the user deletes volunteer "John Doe"
    Then the tournament's volunteer count is 0

  Scenario: Owner views the volunteer list
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with volunteers "John Doe" and "Jane Doe"
    When the user requests the volunteers of "Summer Cup"
    Then the volunteer list contains 2 volunteers

  # --- Validation ---

  Scenario: Adding a volunteer without a name is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user adds a volunteer with an empty name
    Then the request is rejected with 400 Bad Request

  Scenario: Adding a volunteer with duplicate name is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with volunteer "John Doe"
    When the user adds a volunteer with name "John Doe"
    Then the request is rejected with 400 Bad Request

  Scenario: Updating a volunteer with duplicate name is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with volunteers "John Doe" and "Jane Doe"
    When the user updates volunteer "Jane Doe" name to "John Doe"
    Then the request is rejected with 400 Bad Request

  Scenario: Adding a volunteer with a non-existent team is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    When the user adds a volunteer with teamId "non-existent-id"
    Then the request is rejected with 400 Bad Request

  # --- Authorization ---

  Scenario: Admin can manage volunteers on any tournament
    Given user "Admin" has admin role
    And user "Alice" owns tournament "Summer Cup"
    When "Admin" adds a volunteer to "Summer Cup"
    Then the volunteer is added to the tournament

  Scenario: Non-owner cannot add a volunteer
    Given user "Alice" owns tournament "Summer Cup"
    When user "Bob" attempts to add a volunteer to "Summer Cup"
    Then the request is rejected with 403 Forbidden

  Scenario: Anonymous visitor cannot view volunteers
    Given user "Alice" owns tournament "Summer Cup" with volunteer "John Doe"
    When a visitor requests the volunteers of "Summer Cup"
    Then the request is rejected with 401 Unauthorized

  Scenario: Anonymous visitor cannot add a volunteer
    Given a tournament "Summer Cup" exists
    When a visitor attempts to add a volunteer to "Summer Cup"
    Then the request is rejected with 401 Unauthorized

  # --- Team relationship ---

  Scenario: Deleting a team clears volunteer team references
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with team "Eagles"
    And volunteer "John Doe" is linked to team "Eagles"
    When the user deletes team "Eagles"
    Then volunteer "John Doe" has no team assigned

  Scenario: Deleting a tournament deletes all volunteers
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with volunteers "John Doe" and "Jane Doe"
    When the user deletes tournament "Summer Cup"
    Then no volunteers exist for "Summer Cup"
