Feature: Auto-Scheduling
  As a tournament organizer
  I want games to be automatically scheduled across courts and time slots
  So that teams have fair play time, adequate rest, and courts are utilized evenly

  Background:
    Given I am an authenticated tournament owner
    And I have a tournament with courts, teams, groups, and generated games

  # --- Core Scheduling ---

  Scenario: Auto-schedule a phase distributes all games across time slots and courts
    Given a phase with 1 group of 4 teams (6 games) and 2 courts
    When I run auto-schedule for the phase with start time "09:00" and game length 30 minutes
    Then all 6 games should have a start time and court assigned
    And games should be distributed across both courts

  Scenario: No team plays two games in a row (back-to-back constraint)
    Given a phase with 1 group of 4 teams (6 games) and 2 courts
    When I run auto-schedule for the phase
    Then no team should have two consecutive games without at least one time slot gap

  Scenario: No team plays immediately after refereeing (referee rest constraint)
    Given games with referee assignments
    When I run auto-schedule for the phase
    Then no team should play in a time slot immediately after refereeing a game

  Scenario: Games interleaved across groups within a phase
    Given a phase with 2 groups of 3 teams each (3 games per group, 6 total) and 2 courts
    When I run auto-schedule for the phase
    Then games from both groups should be interleaved, not all of one group first

  Scenario: Courts are utilized uniformly
    Given a phase with 1 group of 4 teams (6 games) and 3 courts
    When I run auto-schedule for the phase
    Then each court should have at most 1 more game than any other court

  # --- Break Periods ---

  Scenario: Break periods are respected in the schedule
    Given a phase with 6 games and a break from "10:00" to "10:30"
    When I run auto-schedule for the phase with start time "09:00" and game length 30 minutes
    Then no game should be scheduled during the break period
    And the first game after the break should start at or after "10:30"

  Scenario: Set break periods for a phase
    Given a phase in my tournament
    When I set break periods with start "10:00" and end "10:30"
    Then the phase should have the break period saved

  # --- Schedule Views ---

  Scenario: Get schedule overview for a phase (courts x time grid)
    Given a phase with scheduled games
    When I request the schedule overview for the phase
    Then I should receive a grid of time slots with court assignments and game details

  Scenario: Get team schedule showing all activities
    Given a phase with scheduled games and referee assignments
    When I request the schedule for a specific team
    Then I should see the team's games, refereeing duties, and gaps in chronological order

  # --- Edge Cases ---

  Scenario: Schedule with single court creates sequential slots
    Given a phase with 3 games and 1 court
    When I run auto-schedule for the phase
    Then all games should be on the same court in sequential time slots

  Scenario: Schedule with no generated games should fail
    Given a phase with no games
    When I try to run auto-schedule for the phase
    Then I should receive an error "No games to schedule"

  Scenario: Schedule with no courts should fail
    Given a tournament with no courts
    When I try to run auto-schedule for the phase
    Then I should receive an error "At least one court is required"

  # --- Access Control ---

  Scenario: Anonymous user can view schedule
    Given a phase with a published schedule
    When an anonymous user requests the schedule
    Then they should see the full schedule

  Scenario: Only owner can run auto-schedule
    Given a phase in someone else's tournament
    When I try to run auto-schedule
    Then I should receive a 403 Forbidden error
