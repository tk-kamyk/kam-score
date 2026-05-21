Feature: Team Management

  @FR-TEAM-003 @FR-TEAM-001
  Scenario: Owner adds, updates, and deletes a team
    Given the user owns a tournament
    When the user adds a team with name, level, and contact info
    And updates the team's name and level
    And deletes the team
    Then each change is reflected in the team list

  @FR-TEAM-001
  Scenario Outline: Team validation rejects invalid input
    Given the user owns a tournament with an existing team "Eagles"
    When the user submits <input>
    Then the request is rejected with status 400

    Examples:
      | input                                      |
      | a team named "Eagles" (duplicate)          |
      | a team with level above 100                |
      | a rename of another team to "Eagles"       |

  @FR-USR-011 @FR-USR-001
  Scenario Outline: Team access control
    Given a tournament owned by Alice
    When <actor> tries to <action>
    Then the request is <result>

    Examples:
      | actor               | action             | result                  |
      | Bob (authenticated) | add a team         | rejected with status 403 |
      | an anonymous visitor | add a team        | rejected with status 401 |
      | an anonymous visitor | view the team list | returned successfully    |

  @FR-TEAM-002
  Scenario: Anonymous visitors see teams without contact info
    Given a team with email and phone
    When an anonymous visitor requests the team list
    Then the team is returned without email or phone

  # --- Team Schedule & Participation ---

  @FR-TEAM-023
  Scenario: API filters games by teamId
    Given a tournament with scheduled games
    When games are requested filtered by a specific team
    Then only games where that team plays or referees are returned

  @FR-TEAM-023
  Scenario: Game response includes phase, group, and level names
    Given games scheduled in phases with groups and levels
    When games are requested
    Then each game includes phaseName, groupName, and levelName

  @FR-TEAM-020 @FR-TEAM-021
  Scenario: Team schedule in UI groups games by phase and highlights role
    Given a team participating in multiple phases
    When the user expands the team row
    Then games are grouped under phase headers showing the team's role (Home/Away/Referee)

  @FR-TEAM-022
  Scenario: Team schedule can toggle break rows
    Given a team involved in some but not all time slots
    When "Show breaks" is enabled
    Then break rows are displayed for the time slots where the team has no game

  # --- Generate Seed Teams ---

  @FR-TEAM-010 @FR-TEAM-011 @FR-TEAM-012
  Scenario: Owner generates seed teams
    Given a tournament
    When the owner generates N seed teams
    Then N teams are added named "Seed 1".."Seed N" with proportionally distributed levels
    And generation is additive (names continue from the existing team count)

  @FR-TEAM-010 @FR-TEAM-013
  Scenario Outline: Seed team generation validation and auth
    When <actor> attempts to <action>
    Then the request is rejected with status <status>

    Examples:
      | actor               | action                              | status |
      | the owner           | generate 0 or 101 seed teams        | 400    |
      | a non-owner         | generate seed teams                 | 403    |
      | an anonymous visitor | generate seed teams                | 401    |
