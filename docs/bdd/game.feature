Feature: Game Generation and Management
  As a tournament owner
  I want to generate and schedule games for a phase with a single button
  So that teams can compete according to the tournament format

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with game length 30 minutes
    And the tournament has courts "Court 1" and "Court 2"
    And the tournament has a structure with phases, groups, and assigned teams

  # --- Round Robin ---

  Scenario: Generate and schedule round-robin games for a phase with 3-team groups
    Given a round-robin phase with start time "09:00"
    And group "A" has teams "Eagles", "Hawks", "Wolves"
    When I generate and schedule games for the phase
    Then 3 games should be created for group "A"
    And each team should play 2 games
    And all games should have a court and start time assigned
    And all games should have status "Scheduled"

  Scenario: Generate round-robin games with auto-referee for 3-team group
    Given a round-robin phase with start time "09:00"
    And group "A" has 3 teams
    When I generate and schedule games for the phase
    Then all games in group "A" should have a referee team assigned
    And the referee should not be a team playing in that game

  Scenario: Generate round-robin games with auto-referee for 4-team group
    Given a round-robin phase with start time "09:00"
    And group "A" has 4 teams
    When I generate and schedule games for the phase
    Then 6 games should be created for group "A"
    And all games should have a referee team assigned
    And referee duties should be balanced (each team referees 1-2 times)

  Scenario: Round-robin games have balanced home and away assignments
    Given a round-robin phase with start time "09:00"
    And group "A" has 4 teams
    When I generate and schedule games for the phase
    Then each team should have roughly equal home and away games

  # --- Playoff Elimination ---

  Scenario: Generate and schedule playoff elimination bracket with 4 teams
    Given a playoff-elimination phase with start time "10:00"
    And group "A" has teams "Eagles", "Hawks", "Wolves", "Bears"
    When I generate and schedule games for the phase
    Then 3 games should be created for group "A"
    And 2 semifinal games should have real team IDs
    And 1 final game should have placeholder teams ("Winner SF1" vs "Winner SF2")

  Scenario: Generate playoff elimination with 8 teams
    Given a playoff-elimination phase with start time "10:00"
    And group "A" has 8 teams
    When I generate and schedule games for the phase
    Then 7 games should be created (4 QF + 2 SF + 1 F)
    And quarterfinal games should have real team IDs
    And semifinal and final games should have placeholders

  Scenario: Playoff elimination with 3 teams gives a bye to seed 1
    Given a playoff-elimination phase with start time "10:00"
    And group "A" has teams "Eagles", "Hawks", "Wolves"
    When I generate and schedule games for the phase
    Then 2 games should be created
    And seed 1 should advance directly to the final (bye in round 1)

  # --- Playoff with Placement ---

  Scenario: Generate playoff with full placement for 4 teams
    Given a playoff-with-placement phase with start time "11:00"
    And group "A" has 4 teams
    When I generate and schedule games for the phase
    Then 4 games should be created (2 SF + Final + 3rd place)
    And a 3rd place game should exist with placeholders "Loser SF1" vs "Loser SF2"

  Scenario: Generate playoff with full placement for 8 teams
    Given a playoff-with-placement phase with start time "11:00"
    And group "A" has 8 teams
    When I generate and schedule games for the phase
    Then 12 games should be created for group "A"
    And games should be ordered: QF (round 1) → B-SF (round 2) → A-SF (round 3) → 7th (round 4) → 5th (round 5) → 3rd (round 6) → Final (round 7)
    And the Final should always be in the last round

  # --- Validation ---

  Scenario: Cannot generate games twice for the same phase
    Given games have already been generated for a phase
    When I try to generate and schedule games for the phase
    Then I should receive an error "Games already exist for this phase"

  Scenario: Cannot generate without courts
    Given a tournament with no courts
    When I try to generate and schedule games for the phase
    Then I should receive an error about missing courts

  Scenario: Cannot generate without phase start time
    Given a phase without a start time
    When I try to generate and schedule games for the phase
    Then I should receive an error about missing start time

  Scenario: Cannot generate without tournament game length
    Given a tournament without game length configured
    When I try to generate and schedule games for the phase
    Then I should receive an error about missing game length

  # --- Retrieval ---

  Scenario: Get games for a group
    Given a phase with generated and scheduled games
    When I request games for a specific group
    Then I should see all games with team names, court names, times, and status

  Scenario: Get games filtered by court
    Given a phase with generated and scheduled games
    When I request games filtered by a court ID
    Then I should see only games scheduled on that court

  Scenario: Anonymous user can view games
    Given a phase with generated and scheduled games
    When an anonymous visitor requests games
    Then they should see all games with full details

  # --- Deletion ---

  Scenario: Owner can delete all games for a phase
    Given a phase with generated games
    When the owner deletes games for the phase
    Then all games for that phase should be removed
    And the owner can generate new games for the phase

  Scenario: Non-owner cannot generate or delete games
    Given a phase in someone else's tournament
    When I try to generate or delete games
    Then I should receive a 403 Forbidden error

  # --- Results ---

  Scenario: Participant records a result using tournament code
    Given a scheduled game in a phase with generated games
    When a participant submits set scores [(25,20), (23,25), (15,10)] with a valid tournament code
    Then the game status should change to "Completed"
    And the game should show HomeScore 2 and AwayScore 1
    And the per-set breakdown should be stored

  Scenario: Participant records a simple result (sets won)
    Given a scheduled game in a phase with generated games
    When a participant submits HomeScore 2 and AwayScore 1 with a valid tournament code
    Then the game status should change to "Completed"
    And the game should show HomeScore 2 and AwayScore 1

  Scenario: Recorded result is visible in game list
    Given a game with a recorded result (HomeScore 2, AwayScore 1)
    When anyone requests the games for the tournament
    Then the game should appear with status "Completed", HomeScore 2, and AwayScore 1

  Scenario: Anonymous can view recorded results
    Given a game with a recorded result
    When an anonymous visitor requests games
    Then they should see the result (score and status) in the response

  Scenario: Simple result cannot be a tie
    Given a scheduled game in a phase with generated games
    When a participant submits HomeScore 1 and AwayScore 1 with a valid tournament code
    Then the request should be rejected with status 400
    And the error should indicate a tie is not allowed

  Scenario: Detailed result with exactly one set can be a tie
    Given a scheduled game in a phase with generated games
    When a participant submits set scores [(25,25)] with a valid tournament code
    Then the game status should change to "Completed"
    And the game should show HomeScore 0 and AwayScore 0

  Scenario: Detailed result with more than one set cannot be a tie
    Given a scheduled game in a phase with generated games
    When a participant submits set scores [(25,20), (20,25)] with a valid tournament code
    Then the request should be rejected with status 400
    And the error should indicate a tie is not allowed in a multi-set result

  Scenario: Individual set draw not allowed in multi-set result
    Given a scheduled game in a phase with generated games
    When a participant submits set scores [(25,13), (13,13)] with a valid tournament code
    Then the request should be rejected with status 400
    And the error should indicate each set must have a winner

  Scenario: Owner can edit an already-recorded result
    Given a game with a recorded result (HomeScore 2, AwayScore 1)
    When the owner submits a new result for that game
    Then the game status should remain "Completed"
    And the new result should replace the previous one

  # --- Bracket Advancement ---

  Scenario: Recording a playoff result advances winner to downstream game
    Given a playoff-elimination phase with 4 teams and generated games
    And the semifinal "SF1" is between "Eagles" (home) and "Hawks" (away)
    And the final references "Winner SF1" as the home team placeholder
    When a result is recorded for SF1 with Eagles winning 2-1
    Then the final game's HomeTeamId should be set to "Eagles"
    And the final game's HomeTeamPlaceholder should still be "Winner SF1"

  Scenario: Recording a playoff result advances loser to placement game
    Given a playoff-with-placement phase with 4 teams and generated games
    And the semifinal "SF1" is between "Eagles" and "Hawks"
    And the 3rd-place game references "Loser SF1" as the home team placeholder
    When a result is recorded for SF1 with Eagles winning 2-1
    Then the 3rd-place game's HomeTeamId should be set to "Hawks"

  Scenario: Draw in playoff does not trigger advancement
    Given a playoff-elimination phase with 4 teams and generated games
    And the semifinal "SF1" has a one-set detailed result that is a tie (25-25)
    When the result is recorded
    Then downstream games referencing "Winner SF1" should NOT have their team IDs updated

  Scenario: Correcting a playoff result re-resolves downstream teams
    Given a playoff-elimination phase with 4 teams and generated games
    And SF1 was previously recorded with Eagles winning
    And the final already has HomeTeamId set to "Eagles"
    When the SF1 result is corrected with Hawks winning
    Then the final game's HomeTeamId should be updated to "Hawks"

  Scenario: Round-robin result does not trigger bracket advancement
    Given a round-robin phase with generated games
    When a result is recorded for a round-robin game
    Then no other games should be modified
