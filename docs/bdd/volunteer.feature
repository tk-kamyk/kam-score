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

  # --- Shift Calculation ---

  Scenario: Shift groups include Set-up, all phases, and Cleanup
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And "Summer Cup" has phase "Playoffs" at order 2 with start time "11:00"
    When the user requests shifts for "Summer Cup"
    Then the shift groups are "Set-up", "Pool", "Playoffs", "Cleanup"

  Scenario: Phase shifts are calculated from start time and game length
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And "Summer Cup" has phase "Playoffs" at order 2 with start time "10:00"
    When the user requests shifts for "Summer Cup"
    Then phase "Pool" has shifts at "09:00", "09:20", "09:40"

  Scenario: Partial time slots between phases are dropped
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "10:00"
    And "Summer Cup" has phase "Playoffs" at order 2 with start time "11:30"
    When the user requests shifts for "Summer Cup"
    Then phase "Pool" has shifts at "10:00", "10:20", "10:40", "11:00"
    And phase "Pool" does not have a shift at "11:20"

  Scenario: Last phase shift count equals the number of game rounds
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00" and 4 teams in 1 group
    And games have been generated for "Pool"
    When the user requests shifts for "Summer Cup"
    Then phase "Pool" has 3 shifts

  Scenario: Set-up and Cleanup are always single shifts with no time
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    When the user requests shifts for "Summer Cup"
    Then shift group "Set-up" has 1 shift with no time
    And shift group "Cleanup" has 1 shift with no time

  Scenario: Phase without start time is displayed as a single shift
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with no start time
    When the user requests shifts for "Summer Cup"
    Then phase "Pool" has 1 shift with no time

  Scenario: Missing game length causes all phases to be single shifts
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with no game length
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    When the user requests shifts for "Summer Cup"
    Then phase "Pool" has 1 shift with no time

  # --- Shift Assignment ---

  Scenario: Owner assigns a volunteer to a shift
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteer "John Doe" exists in "Summer Cup"
    When the user assigns "John Doe" to shift "Pool" at "09:00"
    Then "John Doe" is assigned to shift "Pool" at "09:00"

  Scenario: Multiple volunteers can be assigned to the same shift
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteers "John Doe" and "Jane Doe" exist in "Summer Cup"
    When the user assigns "John Doe" to shift "Pool" at "09:00"
    And the user assigns "Jane Doe" to shift "Pool" at "09:00"
    Then shift "Pool" at "09:00" has 2 assigned volunteers

  Scenario: Owner removes a volunteer from a shift
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteer "John Doe" is assigned to shift "Pool" at "09:00"
    When the user removes "John Doe" from shift "Pool" at "09:00"
    Then "John Doe" is not assigned to shift "Pool" at "09:00"

  Scenario: Assigning a volunteer to an invalid shift time is rejected
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteer "John Doe" exists in "Summer Cup"
    When the user assigns "John Doe" to shift "Pool" at "09:15"
    Then the request is rejected with 400 Bad Request

  Scenario: Owner assigns a volunteer to Set-up
    Given the user is authenticated
    And the user owns tournament "Summer Cup"
    And volunteer "John Doe" exists in "Summer Cup"
    When the user assigns "John Doe" to shift "Set-up"
    Then "John Doe" is assigned to shift "Set-up"

  # --- Availability ---

  Scenario: Volunteer whose team plays at shift time is shown as unavailable
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And team "Eagles" has a game at "09:00" in "Summer Cup"
    And volunteer "John Doe" is linked to team "Eagles"
    When the user requests available volunteers for shift "Pool" at "09:00"
    Then "John Doe" is marked as unavailable

  Scenario: Volunteer whose team referees at shift time is shown as unavailable
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And team "Eagles" referees a game at "09:00" in "Summer Cup"
    And volunteer "John Doe" is linked to team "Eagles"
    When the user requests available volunteers for shift "Pool" at "09:00"
    Then "John Doe" is marked as unavailable

  Scenario: Volunteer with no team is always available
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteer "John Doe" has no team in "Summer Cup"
    When the user requests available volunteers for shift "Pool" at "09:00"
    Then "John Doe" is marked as available

  Scenario: Plays-before indicator shown when team plays in previous slot
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And team "Eagles" has a game at "09:00" in "Summer Cup"
    And volunteer "John Doe" is linked to team "Eagles"
    When the user requests available volunteers for shift "Pool" at "09:20"
    Then "John Doe" has playsBefore as true

  Scenario: Plays-after indicator shown when team plays in next slot
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And team "Eagles" has a game at "09:20" in "Summer Cup"
    And volunteer "John Doe" is linked to team "Eagles"
    When the user requests available volunteers for shift "Pool" at "09:00"
    Then "John Doe" has playsAfter as true

  Scenario: Assigned volunteer with conflict shows warning but stays assigned
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteer "John Doe" is linked to team "Eagles" and assigned to shift "Pool" at "09:00"
    And team "Eagles" has a game at "09:00" in "Summer Cup"
    When the user requests shifts for "Summer Cup"
    Then "John Doe" is assigned to shift "Pool" at "09:00"
    And "John Doe" is marked as unavailable for shift "Pool" at "09:00"

  Scenario: Available volunteers sorted by availability, shift count, then name
    Given the user is authenticated
    And the user owns tournament "Summer Cup" with game length 20
    And "Summer Cup" has phase "Pool" at order 1 with start time "09:00"
    And volunteer "Charlie" has 2 shift assignments and is available
    And volunteer "Alice" has 1 shift assignment and is available
    And volunteer "Bob" has 1 shift assignment and is unavailable
    When the user requests available volunteers for shift "Pool" at "09:00"
    Then the order is "Alice", "Charlie", "Bob"
