Feature: Polish, PWA & Structure Copying
  As a tournament organizer
  I want to copy tournament structure from existing tournaments
  And have a polished, mobile-friendly experience
  So that I can reuse formats and run tournaments smoothly on any device

  Background:
    Given I am an authenticated tournament owner

  # --- Structure Copying ---

  Scenario: Copy structure from existing tournament
    Given I have a tournament "Summer Cup 2025" with phases and groups
    When I create a new tournament "Summer Cup 2026" copying structure from "Summer Cup 2025"
    Then the new tournament should have the same phases and groups
    And the new tournament should have no teams or games
    And the new tournament should have a different code

  Scenario: Copy structure copies phase formats and ordering
    Given a source tournament with phases: RoundRobin "Groups" and PlayOffWithPlacement "Finals"
    When I copy the structure
    Then the target should have the same phase names, formats, and ordering

  Scenario: Copy structure from nonexistent tournament returns 404
    When I try to copy structure from a tournament that doesn't exist
    Then I should receive a 404 Not Found

  Scenario: Copy structure requires authentication
    Given a tournament with structure
    When an anonymous user tries to copy its structure
    Then they should receive a 401 Unauthorized

  # --- Tournament Dashboard Overview ---

  Scenario: Dashboard shows tournament progress summary
    Given a tournament with 8 teams, 2 groups, and some completed games
    When I view the tournament overview
    Then I should see team count, group count, and games played vs total

  # --- PWA ---

  Scenario: App is installable as PWA
    Given the SPA is loaded in a browser
    Then a web app manifest should be available
    And the app should register a service worker

  # --- Validation Polish ---

  Scenario: Creating tournament with empty name shows validation error
    When I try to create a tournament with an empty name
    Then I should receive a validation error for the name field

  Scenario: Creating team with level above 100 shows validation error
    When I try to add a team with level 150
    Then I should receive a validation error for the level field

  Scenario: Recording result with negative points shows validation error
    When I try to record a result with negative point values
    Then I should receive a validation error for the points field
