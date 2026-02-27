Feature: Team Auto-Assignment
  As a tournament owner
  I want to auto-assign teams into groups
  So that groups are balanced based on team level

  Background:
    Given I am logged in as the tournament owner
    And a tournament exists with teams and a structure with groups

  Scenario: Auto-assign distributes teams via snake draft by level (first phase)
    Given teams with levels: Eagles(90), Hawks(80), Wolves(70), Bears(60)
    And the first phase has groups "A" and "B"
    When I auto-assign teams to the phase
    Then group "A" should have Eagles(1st) and Bears(4th)
    And group "B" should have Hawks(2nd) and Wolves(3rd)
    # Snake: 1→A, 2→B, 3→B, 4→A (highest level first, snake across groups)

  Scenario: Auto-assign with 3 groups uses snake draft
    Given 6 teams ranked 1-6 by level
    And the first phase has groups "A", "B", "C"
    When I auto-assign teams to the phase
    Then group "A" should have teams ranked 1st and 6th
    And group "B" should have teams ranked 2nd and 5th
    And group "C" should have teams ranked 3rd and 4th
    # Snake: 1→A, 2→B, 3→C, 4→C, 5→B, 6→A

  Scenario: Auto-assign for later phases uses random order
    Given teams with levels: Eagles(90), Hawks(80), Wolves(70), Bears(60)
    And the second phase has groups "A" and "B"
    When I auto-assign teams to the phase
    Then all teams are distributed across the groups
    # Random order instead of level-based for phases after the first

  Scenario: Auto-assign clears previous assignments
    Given teams are already assigned to groups
    When I auto-assign teams to the phase
    Then previous assignments should be replaced with new distribution

  Scenario: Cannot auto-assign with no groups
    Given the phase has no groups
    When I try to auto-assign teams
    Then I should receive a validation error about no groups available

  Scenario: Non-owner cannot auto-assign teams
    Given user "Alice" owns the tournament
    When user "Bob" attempts to auto-assign teams
    Then the request is rejected with 403 Forbidden

  # TBC

  Scenario: Top-together seeding strategy
    # Not yet implemented — sequential fill instead of snake draft
    Given teams with levels: Eagles(90), Hawks(80), Wolves(70), Bears(60)
    And a phase with groups "A" and "B"
    When I seed teams using "TopTogether" strategy
    Then group "A" should have Eagles(1st) and Hawks(2nd)
    And group "B" should have Wolves(3rd) and Bears(4th)

  Scenario: Standings calculation
    # Not yet implemented — depends on game results feature
    Given group "A" has completed games with results
    When I request standings for group "A"
    Then standings should be ordered by: wins → set ratio → point ratio
    And each entry should show: rank, team, wins, losses, sets won/lost, points won/lost
