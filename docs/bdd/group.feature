Feature: Group Management
  As a tournament owner
  I want to manage groups within phases
  So that teams can be organized for play

  Background:
    Given I am logged in as the tournament owner
    And a tournament exists with a structure and a phase

  @FR-STR-030
  Scenario: Owner adds, renames, and removes groups in a phase
    When the owner adds several groups, renames one, and removes another
    Then the phase reflects the current group list

  @FR-STR-031 @FR-STR-033
  Scenario Outline: Group validation rejects invalid operations
    When <operation>
    Then the request is rejected

    Examples:
      | operation                                                      |
      | adding a group with a name already used in the same phase      |
      | assigning the same team to two groups within the same phase    |
      | assigning a team that does not exist in the tournament         |

  @FR-STR-032
  Scenario: Team assignment to and removal from a group
    Given a team and a group both exist
    When the owner assigns the team to the group and later removes it
    Then both changes are reflected in the group's team list

  @FR-USR-001
  Scenario: Anonymous visitor can view groups and team assignments
    Given groups with teams are configured
    When an anonymous visitor views the structure
    Then groups and their assigned teams are returned

  @FR-ADV-044
  Scenario: Editing a phase with resolved placeholders shows real teams
    Given a tournament where the previous phase completed and placeholders have been resolved
    When the owner edits the next phase's groups
    Then real team names are shown and placeholder names do not appear
