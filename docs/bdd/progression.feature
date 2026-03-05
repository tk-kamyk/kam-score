Feature: Phase Advancement
  As a tournament organizer
  I want teams to automatically advance between phases based on standings
  So that the tournament flows from group stage to playoffs seamlessly

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with a structure

  # --- Phase Status ---

  Scenario: New phase has status New
    Given I add a phase "Group Stage" with format RoundRobin
    Then the phase status should be "New"

  Scenario: First phase becomes InProgress when games are generated
    Given a phase "Group Stage" with teams assigned and format RoundRobin
    When I generate and schedule games for the phase
    Then the phase status should be "InProgress"

  Scenario: Subsequent phase becomes InProgress when previous phase completes
    Given a phase "Group Stage" with GroupWinners 2 and TotalTeamsProceeding 4
    And a phase "Playoffs" with format PlayoffElimination
    And all games in "Group Stage" are completed
    When I mark "Group Stage" as complete
    Then "Group Stage" status should be "Completed"
    And "Playoffs" status should be "InProgress"

  # --- Phase Completion ---

  Scenario: Complete a phase when all games are finished
    Given a phase "Group Stage" in status InProgress with all games completed
    When I mark the phase as complete
    Then the phase status should be "Completed"

  Scenario: Cannot complete a phase that is not InProgress
    Given a phase "Group Stage" in status New
    When I try to mark the phase as complete
    Then I should receive an error "Phase must be in progress to complete"

  Scenario: Cannot complete a phase with unfinished games
    Given a phase "Group Stage" in status InProgress with some games still scheduled
    When I try to mark the phase as complete
    Then I should receive an error "All games must be completed before completing the phase"

  # --- Qualifying Teams ---

  Scenario: Top teams per group qualify via GroupWinners
    Given a completed phase "Group Stage" with 2 groups of 4 teams each
    And GroupWinners is set to 2 and TotalTeamsProceeding is set to 4
    When the phase is completed
    Then the top 2 teams from each group should qualify for the next phase

  Scenario: Best remaining teams fill remaining slots
    Given a completed phase "Group Stage" with 2 groups of 4 teams each
    And GroupWinners is set to 1 and TotalTeamsProceeding is set to 4
    When the phase is completed
    Then 1 group winner from each group plus 2 best remaining teams across groups should qualify

  Scenario: Only TotalTeamsProceeding set
    Given a completed phase "Group Stage" with 2 groups of 4 teams each
    And TotalTeamsProceeding is set to 4 (GroupWinners is null)
    When the phase is completed
    Then the top 4 teams across all groups should qualify

  Scenario: Only GroupWinners set
    Given a completed phase "Group Stage" with 2 groups of 4 teams each
    And GroupWinners is set to 2 (TotalTeamsProceeding is null)
    When the phase is completed
    Then the top 2 teams from each group should qualify (4 teams total)

  Scenario: No progression config means no advancement
    Given a completed phase "Group Stage" with neither GroupWinners nor TotalTeamsProceeding set
    When the phase is completed
    Then no teams should advance to the next phase

  # --- Seeding ---

  Scenario: All qualifying teams are ranked in a single seeding order
    Given qualifying teams from a completed phase with 2 groups
    When the seeding is calculated
    Then all qualifying teams should be ranked together by points, set difference, and point difference
    And Seed 1 should be the best performing team overall

  Scenario: Seeded teams are assigned to next phase groups via snake draft
    Given 8 qualifying teams seeded 1 through 8
    And a next phase with 2 groups
    When teams are assigned to the next phase
    Then Group A should contain seeds 1, 4, 5, 8
    And Group B should contain seeds 2, 3, 6, 7

  # --- Placeholder Team Entities ---

  Scenario: Placeholder teams are created when a phase 2+ is added
    Given a phase "Group Stage" with GroupWinners 2 and 2 groups
    When I add a phase "Playoffs" with format PlayoffElimination
    Then 4 placeholder teams should be created
    And each placeholder should have sourcePhaseId matching "Group Stage"
    And placeholder names should follow the pattern "Group Stage - Seed {N}"

  Scenario: Placeholder teams are created based on TotalTeamsProceeding
    Given a phase "Group Stage" with TotalTeamsProceeding 6
    When I add a phase "Round 2" with format RoundRobin
    Then 6 placeholder teams should be created with seeds 1 through 6

  Scenario: No placeholders created when previous phase has no progression config
    Given a phase "Group Stage" with neither GroupWinners nor TotalTeamsProceeding set
    When I add a phase "Playoffs" with format PlayoffElimination
    Then no placeholder teams should be created

  Scenario: Placeholder teams are regenerated when progression config changes
    Given a phase "Group Stage" with GroupWinners 2 and 2 groups
    And a phase "Playoffs" with 4 placeholder teams from "Group Stage"
    When I update "Group Stage" to have GroupWinners 1
    Then old placeholder teams should be deleted
    And 2 new placeholder teams should be created
    And any games in "Playoffs" should be deleted

  Scenario: Placeholder teams are deleted when their source phase is deleted
    Given a phase "Group Stage" with GroupWinners 2 and 2 groups
    And a phase "Playoffs" with placeholder teams from "Group Stage"
    When I delete "Group Stage"
    Then all placeholder teams with sourcePhaseId matching "Group Stage" should be deleted

  # --- Placeholder Assignment ---

  Scenario: Placeholder teams can be assigned to groups manually
    Given a phase "Playoffs" with placeholder teams from the previous phase
    When I assign "Group Stage - Seed 1" to Group A in "Playoffs"
    Then the placeholder team should appear in Group A

  Scenario: Auto-assign distributes placeholder teams by seed via snake draft
    Given a phase "Playoffs" with 4 placeholder teams (seeds 1-4) and 2 groups
    When I auto-assign teams in "Playoffs"
    Then Group A should contain seeds 1 and 4
    And Group B should contain seeds 2 and 3

  Scenario: A placeholder team cannot be assigned to two groups in the same phase
    Given a placeholder team already assigned to Group A in "Playoffs"
    When I try to assign the same placeholder to Group B
    Then the team should not appear in the available teams list

  # --- Game Generation with Placeholders ---

  Scenario: Games are generated with placeholder team IDs
    Given a phase "Playoffs" with placeholder teams assigned to groups
    When I generate games for "Playoffs"
    Then games should have real team IDs (placeholder team IDs, not null)
    And team names in the game response should show placeholder names

  Scenario: Round robin games generated with placeholder teams
    Given a phase "Round 2" with placeholder teams assigned to 2 groups
    When I generate games for "Round 2"
    Then round-robin games should use placeholder team IDs
    And referee assignments should work normally with placeholder teams

  # --- Placeholder Resolution ---

  Scenario: Completing a phase resolves placeholder IDs to real team IDs
    Given a completed phase "Group Stage" with progression configured
    And a next phase "Playoffs" with games using placeholder team IDs
    When I complete "Group Stage"
    Then placeholder team IDs in "Playoffs" games should be swapped to real team IDs
    And placeholder team IDs in "Playoffs" group assignments should be swapped to real team IDs
    And each placeholder team's ResolvedTeamId should point to the real team

  # --- Phase Reopen ---

  Scenario: Reopening a completed phase reverses placeholder resolution
    Given a completed phase "Group Stage" with resolved progression
    And a next phase "Playoffs" in status InProgress with resolved placeholder teams
    When I reopen "Group Stage"
    Then "Group Stage" status should be "InProgress"
    And "Playoffs" status should be "New"
    And real team IDs in "Playoffs" games should be swapped back to placeholder team IDs
    And real team IDs in "Playoffs" group assignments should be swapped back to placeholder team IDs
    And placeholder teams' ResolvedTeamId should be cleared

  # --- Access Control ---

  Scenario: Only owner can complete a phase
    Given a phase in someone else's tournament
    When I try to mark the phase as complete
    Then I should receive a 403 Forbidden error

  Scenario: Only owner can reopen a phase
    Given a completed phase in someone else's tournament
    When I try to reopen the phase
    Then I should receive a 403 Forbidden error
