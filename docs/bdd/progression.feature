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

  # --- Cross-Phase Placeholders ---

  Scenario: Games in subsequent phase are generated with cross-phase placeholders
    Given a phase "Group Stage" with GroupWinners 2 and TotalTeamsProceeding 4
    And a next phase "Playoffs" with format PlayoffElimination and 1 group
    And "Group Stage" is not yet complete
    When I generate games for "Playoffs"
    Then games should be created with placeholders like "Group Stage - Seed 1"
    And team IDs should be null for cross-phase placeholder games

  Scenario: Round robin games generated with cross-phase placeholders
    Given a phase "Group Stage" with TotalTeamsProceeding 6
    And a next phase "Round 2" with format RoundRobin and 2 groups
    When I generate games for "Round 2"
    Then round-robin games should use placeholders like "Group Stage - Seed 1" vs "Group Stage - Seed 4"
    And no referee should be assigned for placeholder games

  # --- Placeholder Resolution ---

  Scenario: Completing a phase resolves placeholders in the next phase
    Given a completed phase "Group Stage" with progression configured
    And a next phase "Playoffs" with games using cross-phase placeholders
    When I complete "Group Stage"
    Then the cross-phase placeholders in "Playoffs" games should be replaced with real team IDs
    And the placeholder strings should still be stored on the games

  # --- Phase Reopen ---

  Scenario: Reopening a completed phase reverts progression
    Given a completed phase "Group Stage" with resolved progression
    And a next phase "Playoffs" in status InProgress with resolved placeholders
    When I reopen "Group Stage"
    Then "Group Stage" status should be "InProgress"
    And "Playoffs" status should be "New"
    And "Playoffs" groups should have no teams assigned
    And cross-phase placeholders in "Playoffs" games should have null team IDs

  # --- Access Control ---

  Scenario: Only owner can complete a phase
    Given a phase in someone else's tournament
    When I try to mark the phase as complete
    Then I should receive a 403 Forbidden error

  Scenario: Only owner can reopen a phase
    Given a completed phase in someone else's tournament
    When I try to reopen the phase
    Then I should receive a 403 Forbidden error
