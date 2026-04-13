Feature: Volunteer Management

  Background:
    Given I am the tournament owner

  # --- CRUD ---

  Scenario: Owner adds, updates, and deletes a volunteer
    When I add a volunteer with name, optional contact, and optional team
    And I update the volunteer's name and contact
    And I delete the volunteer
    Then each change is reflected in the volunteer list

  Scenario Outline: Volunteer validation rejects invalid input
    When I submit a volunteer with <input>
    Then the request is rejected with status 400

    Examples:
      | input                                         |
      | an empty name                                 |
      | a name that duplicates an existing volunteer  |
      | a non-existent team reference                 |

  # --- Authorization ---

  Scenario Outline: Volunteer access is owner/admin-only
    Given another user owns the tournament
    When <actor> attempts to <action>
    Then the request is rejected with status <status>

    Examples:
      | actor               | action                | status |
      | a non-owner         | add a volunteer       | 403    |
      | an admin            | add a volunteer       | 200    |
      | an anonymous visitor | view the volunteers  | 401    |
      | an anonymous visitor | add a volunteer      | 401    |

  # --- Team relationship ---

  Scenario: Deleting a team clears volunteer team references
    Given a volunteer is linked to a team
    When the team is deleted
    Then the volunteer's team reference is cleared (but the volunteer is kept)

  Scenario: Deleting a tournament deletes all volunteers
    Given a tournament has volunteers
    When the tournament is deleted
    Then no volunteers exist for that tournament

  # --- Shift calculation (shift math is unit-tested; these cover behaviour) ---

  Scenario: Shift groups always include Set-up, phases, and Cleanup
    Given a tournament with configured phases
    When shifts are requested
    Then shift groups are returned in order: Set-up, each phase, Cleanup

  Scenario Outline: Phase shifts are derived from start time and game length
    Given a phase with <config>
    When shifts are requested
    Then the phase's shifts match <outcome>

    Examples:
      | config                                                   | outcome                                              |
      | start time and a next phase's start time                 | evenly stepped slots, dropping any partial last slot |
      | start time but the game length is missing                | a single untimed shift                               |
      | no start time                                            | a single untimed shift                               |
      | start time and is the last phase                         | one shift per game round in the phase                |

  # --- Shift assignment ---

  Scenario: Owner assigns and removes volunteers for shifts
    Given a volunteer and a shift exist
    When the owner assigns the volunteer and later removes them
    Then the assignment is stored and cleared respectively

  Scenario: Multiple volunteers can be assigned to the same shift
    Given two volunteers exist
    When both are assigned to the same shift
    Then the shift reflects both assignments

  Scenario: Assigning to an invalid shift time is rejected
    Given a phase with computed shift slots
    When the owner assigns a volunteer to a time that is not a valid slot
    Then the request is rejected with status 400

  # --- Availability ---

  Scenario Outline: Availability reflects the volunteer's linked team
    Given a volunteer linked to a team
    When the team <activity> at the shift time
    Then the volunteer is marked as <status>

    Examples:
      | activity                                | status      |
      | plays                                   | unavailable |
      | referees                                | unavailable |
      | plays in the previous slot              | playsBefore |
      | plays in the next slot                  | playsAfter  |
      | has no game at the shift time           | available   |

  Scenario: Volunteer with no linked team is always available
    Given a volunteer has no linked team
    When availability is requested
    Then the volunteer is marked as available

  Scenario: Conflicting assignment is kept with a warning
    Given a volunteer is assigned to a shift
    When a game is later scheduled that conflicts with the shift
    Then the assignment is kept but the volunteer is shown as unavailable

  Scenario: Available volunteers are sorted by availability, shift count, then name
    Given volunteers with varying availability and shift counts
    When the owner requests available volunteers
    Then available volunteers come first, then by fewest shifts, then alphabetically
