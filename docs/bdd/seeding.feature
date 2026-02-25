Feature: Team Seeding
  As a tournament owner
  I want to seed teams into groups using different strategies
  So that groups are balanced or arranged as desired

  Background:
    Given I am logged in as the tournament owner
    And a tournament exists with teams and a structure with groups

  Scenario: Standard seeding (snake draft by level)
    Given teams with levels: Eagles(90), Hawks(80), Wolves(70), Bears(60)
    And a phase with groups "A" and "B"
    When I seed teams using "Standard" strategy
    Then group "A" should have Eagles(1st) and Bears(4th)
    And group "B" should have Hawks(2nd) and Wolves(3rd)
    # Snake: 1→A, 2→B, 3→B, 4→A (highest level first, snake across groups)

  Scenario: Standard seeding with 3 groups
    Given 6 teams ranked 1-6 by level
    And a phase with groups "A", "B", "C"
    When I seed teams using "Standard" strategy
    Then group "A" should have teams ranked 1st and 6th
    And group "B" should have teams ranked 2nd and 5th
    And group "C" should have teams ranked 3rd and 4th
    # Snake: 1→A, 2→B, 3→C, 4→C, 5→B, 6→A

  Scenario: Top-together seeding
    Given teams with levels: Eagles(90), Hawks(80), Wolves(70), Bears(60)
    And a phase with groups "A" and "B"
    When I seed teams using "TopTogether" strategy
    Then group "A" should have Eagles(1st) and Hawks(2nd)
    And group "B" should have Wolves(3rd) and Bears(4th)
    # Sequential: fill groups in order from top-ranked

  Scenario: Seeding clears previous assignments
    Given teams are already assigned to groups
    When I seed teams using "Standard" strategy
    Then previous assignments should be replaced with new seeding

  Scenario: Cannot seed with no teams
    Given the tournament has no teams
    When I try to seed teams
    Then I should receive an error about no teams available

  Scenario: Cannot seed with no groups
    Given the phase has no groups
    When I try to seed teams
    Then I should receive an error about no groups available

  Scenario: Seeding sets the seeding strategy on the phase
    When I seed teams using "Standard" strategy
    Then the phase seeding strategy should be "Standard"

  Scenario: Standings calculation
    Given group "A" has completed games with results
    When I request standings for group "A"
    Then standings should be ordered by: wins → set ratio → point ratio
    And each entry should show: rank, team, wins, losses, sets won/lost, points won/lost
