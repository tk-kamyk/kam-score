Feature: Phase Progression & Brackets
  As a tournament organizer
  I want teams to advance between phases based on standings
  So that winners from group stage proceed to playoffs automatically

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with structure, teams, and completed group phase games

  # --- Progression Rules ---

  Scenario: Set progression rules mapping group positions to next phase
    Given a group phase "Group Stage" with groups A and B
    And a playoff phase "Playoffs" with group "Bracket"
    When I set progression rules:
      | Source               | Target Phase | Target Group |
      | GroupStage-A-1       | Playoffs     | Bracket      |
      | GroupStage-A-2       | Playoffs     | Bracket      |
      | GroupStage-B-1       | Playoffs     | Bracket      |
      | GroupStage-B-2       | Playoffs     | Bracket      |
    Then the group phase should have 4 progression rules saved

  # --- Progression Execution ---

  Scenario: Progress teams from group stage to playoffs
    Given a group phase with completed games and standings
    And progression rules mapping top 2 from each group to a playoff phase
    When I trigger progression for the group phase
    Then the top 2 teams from each group should appear in the playoff phase groups
    And the progression should be marked as completed

  Scenario: Progression fails if source phase games are not complete
    Given a group phase with some games still scheduled
    And progression rules are set
    When I try to trigger progression
    Then I should receive an error "All games must be completed before progression"

  Scenario: Progression fails if no progression rules exist
    Given a group phase with completed games
    And no progression rules
    When I try to trigger progression
    Then I should receive an error "No progression rules defined"

  Scenario: Re-progression clears and re-assigns teams
    Given a group phase with completed progression
    When I trigger progression again (after result corrections)
    Then the target phase teams should be updated to reflect current standings

  # --- Bracket Generation ---

  Scenario: Generate elimination bracket from progressed teams
    Given a playoff phase with 4 teams in a single group
    And the phase format is PlayOffElimination
    When I generate bracket games
    Then semifinal games should be created (1st vs 4th, 2nd vs 3rd)

  Scenario: Generate bracket with placement games
    Given a playoff phase with 4 teams
    And the phase format is PlayOffWithPlacement
    When I generate bracket games
    Then semifinal games should be created
    And a 3rd-place game should be created for semifinal losers

  # --- Bracket View ---

  Scenario: Get bracket view for a playoff phase
    Given a playoff phase with bracket games
    When I request the bracket view
    Then I should receive rounds with match pairings and advancement paths

  # --- Access Control ---

  Scenario: Anonymous user can view bracket
    Given a playoff phase with bracket games
    When an anonymous user requests the bracket
    Then they should see the bracket rounds and games

  Scenario: Only owner can trigger progression
    Given a phase in someone else's tournament
    When I try to trigger progression
    Then I should receive a 403 Forbidden error
