Feature: Phase State Restrictions
  As a tournament owner
  I want the system to prevent invalid operations on phases
  So that tournament data integrity is maintained

  Background:
    Given I am authenticated as the tournament owner
    And a tournament with an initialized structure exists

  # Restriction 1: Completed phase cannot be edited
  Scenario: Cannot edit a completed phase
    Given a phase "Group Stage" with status "Completed"
    When I try to update the phase name to "Modified Name"
    Then the request is rejected with status 409
    And the error message contains "phase is completed"

  # Restriction 2: Phase with games cannot have structure edited
  Scenario: Cannot change phase format when games exist
    Given a phase "Group Stage" with status "InProgress" and generated games
    When I try to change the phase format to "PlayoffElimination"
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Can change phase name when games exist
    Given a phase "Group Stage" with status "InProgress" and generated games
    When I update the phase name to "Round Robin Stage"
    Then the request succeeds with status 200

  Scenario: Can change progression fields when games exist
    Given a phase "Group Stage" with status "InProgress" and generated games
    When I update the phase with groupWinners 2 and totalTeamsProceeding 6
    Then the request succeeds with status 200
    And the phase has groupWinners 2 and totalTeamsProceeding 6

  Scenario: Cannot change start time when games exist
    Given a phase "Group Stage" with status "InProgress", start time "09:00", and generated games
    When I try to change the start time to "14:00"
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Cannot delete a phase with generated games
    Given a phase "Group Stage" with status "InProgress" and generated games
    When I try to delete the phase
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Cannot delete a completed phase
    Given a phase "Group Stage" with status "Completed"
    When I try to delete the phase
    Then the request is rejected with status 409
    And the error message contains "phase is completed"

  Scenario: Cannot auto-assign teams when games exist
    Given a phase "Group Stage" with groups and generated games
    When I try to auto-assign teams
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Cannot add a group to a completed phase
    Given a phase "Group Stage" with status "Completed"
    When I try to add a group "Group C"
    Then the request is rejected with status 409
    And the error message contains "phase is completed"

  Scenario: Cannot add a group when games exist
    Given a phase "Group Stage" with status "InProgress" and generated games
    When I try to add a group "Group C"
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Cannot rename a group in a completed phase
    Given a phase "Group Stage" with status "Completed" and a group "A"
    When I try to rename the group to "Winners"
    Then the request is rejected with status 409
    And the error message contains "phase is completed"

  Scenario: Cannot delete a group when games exist
    Given a phase "Group Stage" with a group "A" and generated games
    When I try to delete the group
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Cannot assign a team when games exist
    Given a phase "Group Stage" with a group "A" and generated games
    When I try to assign a team to the group
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  Scenario: Cannot remove a team when games exist
    Given a phase "Group Stage" with a group "A" containing team "Eagles" and generated games
    When I try to remove team "Eagles" from the group
    Then the request is rejected with status 409
    And the error message contains "games have been generated"

  # Restriction 3: Block recording results for completed phase
  Scenario: Cannot record results in a completed phase
    Given a phase "Group Stage" with status "Completed"
    And a game in the phase
    When I try to record a result for the game
    Then the request is rejected with status 409
    And the error message contains "phase is completed"

  # Restriction 4: Reset phase status on game deletion
  Scenario: Cannot delete games from a completed phase
    Given a phase "Group Stage" with status "Completed"
    When I try to delete the phase games
    Then the request is rejected with status 409
    And the error message contains "phase is completed"

  Scenario: Deleting games resets phase status to New
    Given a phase "Group Stage" with status "InProgress" and generated games
    When I delete the phase games
    Then the request succeeds with status 204
    And the phase status is reset to "New"

  # Restriction 5: Block reopen when next phase has completed games
  Scenario: Cannot reopen phase when next phase has completed games
    Given a completed phase "Group Stage" followed by phase "Playoffs"
    And phase "Playoffs" has games with recorded results
    When I try to reopen phase "Group Stage"
    Then the request is rejected with status 409
    And the error message contains "next phase has completed games"

  # Restriction 6: Block recording results when teams unassigned
  Scenario: Cannot record result when teams are not yet assigned
    Given a playoff game with unresolved team placeholders
    When I try to record a result for the game
    Then the request is rejected with status 400
    And the error message contains "both teams must be assigned"

  # Restriction 7: Block deleting team referenced in games or groups
  Scenario: Cannot delete a team assigned to a group
    Given a team "Eagles" assigned to a group in phase "Group Stage"
    When I try to delete team "Eagles"
    Then the request is rejected with status 409
    And the error message contains "team is assigned to a group"

  Scenario: Cannot delete a team referenced in games
    Given a team "Eagles" playing in a scheduled game
    When I try to delete team "Eagles"
    Then the request is rejected with status 409
    And the error message contains "team is referenced in games"

  # Restriction 8: Block deleting court referenced in games
  Scenario: Cannot delete a court with scheduled games
    Given a court "Court 1" with scheduled games
    When I try to delete court "Court 1"
    Then the request is rejected with status 409
    And the error message contains "court has scheduled games"
