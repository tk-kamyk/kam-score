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
