Feature: Feature Flags
  As a system operator
  I want to configure feature flags via appsettings
  So that incomplete features can be hidden in production while visible in development

  Scenario: Retrieve feature flags when none are configured
    Given no feature flags are configured
    When I request GET /api/feature-flags
    Then the response status should be 200
    And the response body should be an empty JSON object

  Scenario: Retrieve configured feature flags
    Given the following feature flags are configured:
      | Flag         | Enabled |
      | LiveScoring  | true    |
      | Referees     | false   |
    When I request GET /api/feature-flags
    Then the response status should be 200
    And the response body should contain "LiveScoring" with value true
    And the response body should contain "Referees" with value false

  Scenario: Feature flags endpoint requires no authentication
    Given the following feature flags are configured:
      | Flag        | Enabled |
      | LiveScoring | true    |
    When I request GET /api/feature-flags without authentication
    Then the response status should be 200
    And the response body should contain "LiveScoring" with value true

  Scenario: Frontend treats missing flags as disabled
    Given the following feature flags are configured:
      | Flag        | Enabled |
      | LiveScoring | true    |
    When the frontend checks if "UnknownFeature" is enabled
    Then the result should be false
