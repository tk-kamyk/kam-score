Feature: Feature Flags (mechanism)
  # Retained as intentional boilerplate (no active flags today).
  # See docs/requirements/feature-flags.md.

  @FR-FF-001 @FR-FF-002
  Scenario Outline: Feature flags endpoint exposes configured flags
    Given flags are <configuration>
    When GET /api/feature-flags is called without authentication
    Then the response is 200 with <payload>

    Examples:
      | configuration                            | payload                          |
      | not configured                           | an empty JSON object             |
      | configured as a mix of true/false values | those values as a JSON object    |

  @FR-FF-003 @FR-FF-004
  Scenario: Frontend treats unknown flags as disabled
    Given a known flag is enabled
    When the frontend checks an unknown flag
    Then the check returns false
