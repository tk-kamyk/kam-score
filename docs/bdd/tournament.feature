Feature: Tournament Management

  Scenario: Authenticated user creates a tournament
    Given the user is authenticated
    When the user creates a tournament with name, discipline, and game length
    Then the tournament is created
    And the tournament has an auto-generated code
    And the tournament appears in the user's tournament list

  Scenario: User creates a tournament with game conditions
    Given the user is authenticated
    When the user creates a tournament with best-of-sets and per-set points specified
    Then the tournament game conditions are stored correctly

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

  Scenario: Owner updates and deletes their tournament
    Given the user owns a tournament
    When the user updates the tournament name
    And then deletes the tournament
    Then the name change is persisted before deletion
    And the tournament no longer appears in the list

  Scenario Outline: Non-owner cannot modify another user's tournament
    Given user "Alice" owns tournament "Cup A"
    When user "Bob" attempts to <operation> "Cup A"
    Then the request is rejected with 403 Forbidden

    Examples:
      | operation |
      | update    |
      | delete    |

  Scenario: Anyone can list and view tournaments
    Given several tournaments exist owned by different users
    When any authenticated or anonymous user requests the tournament list or details
    Then the data is returned successfully (codes hidden as described above)

  # --- Copy Structure ---

  Scenario: Create tournament by copying structure from another tournament
    Given a source tournament with courts, real teams, and a multi-phase structure
    When the user creates a new tournament copying structure from the source
    Then the new tournament has the same discipline, game length, start time, courts, and phase layout
    And the real teams are replaced with seed teams (Seed 1..N) matching the source's real team count
    And no volunteers, results, or tournament code are carried over
    And games are generated for phases whose prerequisites are met
    And phase 1 is InProgress while later phases are Scheduled

  Scenario: Copy skips game generation when prerequisites are missing
    Given a source tournament with a phase missing prerequisites (no start time, no courts, or no teams)
    When the user copies the structure
    Then the affected phase has no games generated and remains in status New

  Scenario: Any authenticated user can copy from any tournament
    Given user "Alice" owns a tournament
    When user "Bob" creates a tournament copying Alice's structure
    Then Bob's tournament is created successfully owned by Bob

  Scenario: Copying a nonexistent tournament returns 404
    When the user tries to copy structure from a tournament that doesn't exist
    Then the request is rejected with status 404
