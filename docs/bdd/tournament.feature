Feature: Tournament Management

  @FR-TRN-005 @FR-TRN-001
  Scenario: Authenticated user creates a tournament
    Given the user is authenticated
    When the user creates a tournament with name, discipline, and game length
    Then the tournament is created
    And the tournament has an auto-generated code
    And the tournament appears in the user's tournament list

  @FR-TRN-004
  Scenario: User creates a tournament with game conditions
    Given the user is authenticated
    When the user creates a tournament with best-of-sets and per-set points specified
    Then the tournament game conditions are stored correctly

  @FR-TRN-008
  Scenario Outline: Tournament code visibility follows ownership
    Given user "Alice" owns tournament "Cup A"
    When <actor> requests <context>
    Then the tournament code <visibility> visible

    Examples:
      | actor           | context                      | visibility |
      | Alice           | the details of "Cup A"       | is         |
      | Alice           | the tournament list          | is         |
      | an anonymous visitor | the details of "Cup A"  | is not     |
      | an anonymous visitor | the tournament list     | is not     |
      | user "Bob"      | the details of "Cup A"       | is not     |

  @FR-TRN-005 @FR-TRN-009
  Scenario: Owner updates and deletes their tournament
    Given the user owns a tournament
    When the user updates the tournament name
    And then deletes the tournament
    Then the name change is persisted before deletion
    And the tournament no longer appears in the list

  @FR-TRN-009
  Scenario Outline: Non-owner cannot modify another user's tournament
    Given user "Alice" owns tournament "Cup A"
    When user "Bob" attempts to <operation> "Cup A"
    Then the request is rejected with 403 Forbidden

    Examples:
      | operation |
      | update    |
      | delete    |

  @FR-TRN-006 @FR-TRN-033
  Scenario Outline: Tournament list visibility by type and viewer
    Given Alice owns a Public tournament, a Private tournament, and a Template tournament
    When <viewer> requests the tournament list
    Then the list contains <result>

    Examples:
      | viewer               | result                                                 |
      | an anonymous visitor | Alice's Public tournament only                         |
      | Alice                | all of Alice's tournaments (Public, Private, Template) |
      | another owner Bob    | Alice's Public tournament only                         |
      | an admin             | all of Alice's tournaments                             |

  # --- Copy Structure ---

  @FR-TRN-020 @FR-TRN-021 @FR-TRN-022 @FR-TRN-025
  Scenario: Create tournament by copying structure from another tournament
    Given a source tournament with courts, real teams, and a multi-phase structure
    When the user creates a new tournament copying structure from the source
    Then the new tournament has the same discipline, game length, start time, courts, and phase layout
    And the real teams are replaced with seed teams (Seed 1..N) matching the source's real team count
    And no volunteers, results, or tournament code are carried over
    And games are generated for phases whose prerequisites are met
    And phase 1 is InProgress while later phases are Scheduled

  @FR-TRN-025
  Scenario: Copy skips game generation when prerequisites are missing
    Given a source tournament with a phase missing prerequisites (no start time, no courts, or no teams)
    When the user copies the structure
    Then the affected phase has no games generated and remains in status New

  @FR-TRN-020
  Scenario: Any authenticated user can copy from any tournament
    Given user "Alice" owns a tournament
    When user "Bob" creates a tournament copying Alice's structure
    Then Bob's tournament is created successfully owned by Bob

  @FR-TRN-020
  Scenario: Copying a nonexistent tournament returns 404
    When the user tries to copy structure from a tournament that doesn't exist
    Then the request is rejected with status 404

  # --- Tournament Type & Visibility ---

  @FR-TRN-030 @FR-TRN-031 @FR-TRN-032
  Scenario: Type is chosen at creation, shown as a badge, and editable
    Given the user is authenticated
    When the user creates a tournament with a specific type
    Then the tournament is created with the selected type
    And its type is shown as a badge on the tournament list and details page
    When the user edits the tournament and changes its type
    Then the tournament type is updated to the new type

  @FR-TRN-033
  Scenario Outline: Private and Template tournaments stay reachable by direct link
    Given Alice owns a <type> tournament
    When an anonymous visitor opens the tournament's direct link (details by id)
    Then the tournament details are returned successfully
    And the tournament code is not visible

    Examples:
      | type     |
      | Private  |
      | Template |

  @FR-TRN-034 @FR-TRN-020
  Scenario: Copy-structure sources include all templates but only the viewer's own private
    Given Alice owns a Public, a Private, and a Template tournament
    And Bob owns a Template tournament and a Private tournament
    When Alice requests the available copy-structure sources
    Then the sources include all Public tournaments
    And the sources include every Template tournament, including Bob's
    And the sources include Alice's own Private tournament
    And the sources exclude Bob's Private tournament
