Feature: UX Polish and PWA

  # These scenarios cover cross-cutting UX behaviours that no specific
  # functional requirement currently captures. Tagged loosely; consider
  # adding explicit FRs if behaviours grow.

  @FR-USR-001
  Scenario: Dashboard shows tournament progress summary
    Given a tournament with teams, groups, and some completed games
    When I view the tournament overview
    Then I see team count, group count, and games played vs total

  Scenario: App is installable as a PWA
    Given the SPA is loaded in a browser
    Then a web app manifest is available
    And the app registers a service worker

  Scenario Outline: Input validation surfaces field errors
    When I submit <input>
    Then a validation error is shown for the relevant field

    Examples:
      | input                                    |
      | a tournament with an empty name          |
      | a team with level above 100              |
      | a result with negative point values      |
