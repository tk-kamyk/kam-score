Feature: Participant Access via Tournament Code
  As a tournament participant
  I want to record game results using a tournament code
  So that I don't need to create an account

  Background:
    Given a tournament with code "XKRT3" and active games

  Scenario: Record result with valid tournament code
    Given a scheduled game between "Eagles" and "Hawks"
    When I submit a result with X-Tournament-Code header "XKRT3"
    Then the result should be recorded successfully

  Scenario: Record result with invalid tournament code
    When I submit a result with X-Tournament-Code header "WRONG"
    Then I should receive a 403 Forbidden response

  Scenario: Record result without any auth
    When I submit a result without auth token or tournament code
    Then I should receive a 401 Unauthorized response

  Scenario: Tournament code only grants result recording
    Given a valid tournament code "XKRT3"
    When I try to create a team using the tournament code
    Then I should receive a 401 Unauthorized response

  Scenario: Tournament code cannot modify structure
    Given a valid tournament code "XKRT3"
    When I try to add a phase using the tournament code
    Then I should receive a 401 Unauthorized response

  Scenario: Code-based access is case-insensitive
    Given a tournament with code "XKRT3"
    When I submit a result with code "xkrt3"
    Then the result should be recorded successfully

  Scenario: Record result in detailed set format
    Given a scheduled game between "Eagles" and "Hawks"
    When I submit a result with X-Tournament-Code header "XKRT3" and sets [(25,20), (23,25), (15,10)]
    Then the result should be recorded successfully
    And the game should show HomeScore 2, AwayScore 1, status "Completed"
    And the per-set breakdown should be stored

  Scenario: Record result in simple mode (sets won only)
    Given a scheduled game between "Eagles" and "Hawks"
    When I submit a result with X-Tournament-Code header "XKRT3" and HomeScore 2, AwayScore 1
    Then the result should be recorded successfully
    And the game should show HomeScore 2, AwayScore 1, status "Completed"

  Scenario: Authenticated owner can record result without tournament code
    Given a scheduled game between "Eagles" and "Hawks"
    When the tournament owner submits a result with a valid JWT token
    Then the result should be recorded successfully

  Scenario: Non-owner authenticated user cannot record result without tournament code
    Given a user authenticated as a different owner
    When they try to record a result without a tournament code
    Then I should receive a 403 Forbidden response
