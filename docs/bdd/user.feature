Feature: User Authentication

  Scenario: Visitor accesses public tournament data without authentication
    Given a tournament "Summer Cup" exists
    When a visitor requests the tournament list
    Then the tournament list is returned successfully

  Scenario: Visitor cannot create data without authentication
    When a visitor attempts to create a tournament
    Then the request is rejected with 401 Unauthorized

  Scenario: Visitor cannot update data without authentication
    Given a tournament "Summer Cup" exists
    When a visitor attempts to update the tournament
    Then the request is rejected with 401 Unauthorized

  Scenario: Visitor cannot delete data without authentication
    Given a tournament "Summer Cup" exists
    When a visitor attempts to delete the tournament
    Then the request is rejected with 401 Unauthorized

  Scenario: Authenticated user can create data
    Given the user is authenticated
    When the user creates a tournament with name "Summer Cup"
    Then the tournament is created successfully

  # Admin role scenarios

  Scenario: Admin can update another user's tournament
    Given user "Alice" owns tournament "Summer Cup"
    And user "AdminUser" is authenticated with role "Admin"
    When AdminUser updates tournament "Summer Cup"
    Then the tournament is updated successfully

  Scenario: Admin can delete another user's tournament
    Given user "Alice" owns tournament "Summer Cup"
    And user "AdminUser" is authenticated with role "Admin"
    When AdminUser deletes tournament "Summer Cup"
    Then the tournament is deleted successfully

  Scenario: Admin sees tournament codes for all tournaments
    Given user "Alice" owns tournament "Cup A"
    And user "AdminUser" is authenticated with role "Admin"
    When AdminUser requests the tournament list
    Then "Cup A" includes the tournament code

  Scenario: Admin sees team contact info for any tournament
    Given user "Alice" owns tournament "Summer Cup" with team "Eagles" (email: "eagles@example.com", phone: "+123456789")
    And user "AdminUser" is authenticated with role "Admin"
    When AdminUser requests the teams of "Summer Cup"
    Then the team email and phone are visible

  Scenario: Admin can record game results without tournament code
    Given user "Alice" owns tournament "Summer Cup" with a scheduled game
    And user "AdminUser" is authenticated with role "Admin"
    When AdminUser records a game result for "Summer Cup"
    Then the game result is saved successfully

  Scenario: Login response includes user role
    Given a user "AdminUser" exists with role "Admin"
    When AdminUser logs in with valid credentials
    Then the login response includes role "Admin"

  Scenario: Login response includes default role for regular user
    Given a user "RegularUser" exists with role "User"
    When RegularUser logs in with valid credentials
    Then the login response includes role "User"
