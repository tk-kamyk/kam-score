Feature: Group Management
  As a tournament owner
  I want to manage groups within phases
  So that teams can be organized for play

  Background:
    Given I am logged in as the tournament owner
    And a tournament exists with a structure and a "Group Stage" phase (RoundRobin)

  Scenario: Add a group to a phase
    When I add a group named "A" to the "Group Stage" phase
    Then the group "A" should appear in the phase
    And the group should have no teams assigned

  Scenario: Add multiple groups to a phase
    When I add groups "A", "B", "C" to the "Group Stage" phase
    Then the phase should have 3 groups

  Scenario: Cannot add duplicate group name in same phase
    Given a group "A" already exists in the "Group Stage" phase
    When I try to add another group named "A"
    Then I should receive an error "already exists"

  Scenario: Remove a group from a phase
    Given groups "A" and "B" exist in the "Group Stage" phase
    When I remove group "A"
    Then only group "B" should remain in the phase

  Scenario: Assign a team to a group
    Given a group "A" exists in the "Group Stage" phase
    And a team "Eagles" exists in the tournament
    When I assign "Eagles" to group "A"
    Then group "A" should contain team "Eagles"

  Scenario: Remove a team from a group
    Given a group "A" exists with team "Eagles" assigned
    When I remove "Eagles" from group "A"
    Then group "A" should have no teams assigned

  Scenario: Cannot assign same team to two groups in same phase
    Given groups "A" and "B" exist in the "Group Stage" phase
    And "Eagles" is assigned to group "A"
    When I try to assign "Eagles" to group "B"
    Then I should receive an error about duplicate team assignment

  Scenario: Cannot assign a team that doesn't exist in tournament
    Given a group "A" exists in the "Group Stage" phase
    When I try to assign a non-existent team to group "A"
    Then I should receive an error "not found"

  Scenario: Visitor can view groups and team assignments
    Given groups with teams are configured
    When an anonymous visitor views the structure
    Then they should see all groups and their team assignments
