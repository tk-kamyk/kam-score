# Structure

- A tournament has one structure, initialized explicitly by the owner
- The structure contains an ordered list of phases
- Structure overview is available to everyone; editing requires authentication

# Phase

- Phase defines a part of tournament
- Phase is represented by:
    - name
    - format of the games (round robin, play-off elimination, play-off with placement games, double elimination)
    - number of groups (specified on creation, auto-named A, B, C...)
    - group winners (optional) — how many teams per group qualify automatically to the next phase
    - total teams proceeding (optional) — total number of teams qualifying from this phase (a combined ranking is built: group winners on top, then remaining teams, cutoff applied at total)
    - start time (optional) — baseline time for scheduling games in this phase (HH:mm format)
- Phases are ordered sequentially (1, 2, 3...) and automatically reordered when one is deleted
- Teams are assigned to groups via auto-assign (snake draft based on team level for first phase, random for later phases) or manually
- Manual assignment can override auto-assign; retriggering auto-assign resets manual edits
- In each phase, each position after the game is played is assigned a unique identifier, e.g. phaseA-groupA-position1
- Output from one phase (positions after the games) is input (seeded teams) to another phase
- Phase edit is a view dedicated to the authenticated users
- Once the phases are saved, a dedicated phase overview is available to everyone

# Group

- Group is represented by:
    - name
    - teams
    - games
    - standings
- Group name is unique within one phase
- Teams can be assigned to and removed from groups individually
- A team cannot be assigned to two groups in the same phase

# Game

- Game is defined by
    - the home team (or placeholder for playoff)
    - the away team (or placeholder for playoff)
    - the refereeing team
    - the court
    - the start time
    - the result (optional — recorded after the game is played)
- Games should not be created manually
- A single "Generate & Schedule" button generates all games in a phase and auto-schedules them across courts and time slots
- Game generation depends on the phase format:
    - **Round Robin**: all-play-all within each group. Uses circle method for pairings with home/away balance
    - **Playoff Elimination**: single-elimination bracket per group. First round uses real team IDs (seeded). Later rounds use placeholders (e.g., "Winner SF1", "Winner QF2")
    - **Playoff with Placement**: single-elimination bracket with full placement games for all final positions (1st through Nth). After each elimination round, losers form a consolation bracket (B) and winners form the main bracket (A). Consolation rounds are always scheduled before main bracket rounds at each level. Placement games (final position matches) are ordered worst-to-best position, with the Final always last. Example for 8 teams: QF → B-SF (QF losers) → A-SF (QF winners) → 7th → 5th → 3rd → Final
    - **Double Elimination (VD)**: a volleyball-specific variant of double elimination for exactly 8 teams per group. Structure: 4 QFs (seeded 1v8, 4v5, 2v7, 3v6) → 2 QF Winners games → 2 QF Losers games → 2 Crossover games (cross-bracket: Loser W2 vs Winner L1, Loser W1 vs Winner L2) → 2 Grand SFs (same-half: Winner W1 vs Winner X1, Winner W2 vs Winner X2) → 7th Place game (two 0-2 teams) → Grand Final. Total: 14 games, 7 rounds. Positions: 1st/2nd from Grand Final, 3rd-4th shared (Grand SF losers), 5th-6th shared (Crossover losers), 7th/8th from 7th Place game. Validation: rejects groups with ≠ 8 teams
    - **Double Elimination**: teams must lose twice to be eliminated. Consists of a Winners Bracket (WB), Losers Bracket (LB), and a Grand Final. WB is standard single-elimination; losers drop to LB. LB alternates between rounds where WB losers enter (drop-down) and rounds where LB teams play each other. The Grand Final is a single game between the WB winner and LB winner (no reset match). Example for 8 teams: WB-QF (4 games) → WB-SF (2 games) → WB-Final (1 game); LB-R1 (2 games, QF losers) → LB-R2 (2 games, LB-R1 winners vs SF losers) → LB-R3 (1 game) → LB-R4 (1 game, vs WB-Final loser) → Grand Final
- Playoffs and double elimination apply to each group individually; bracket size is determined by the number of teams in the group
- Scheduling uses the tournament-level game length (minutes) for time slot duration and the phase-level start time as the baseline

# Schedule

- The games should be uniformly distributed among courts
- The games within each phase should be uniformly distributed among groups (e.g. the first game should happen at the same time in each group if possible due to amount of courts)
- No team should play or referee in two games at the same time
- A team must have a free time slot before any game they play in (no activity — playing or refereeing — in the immediately preceding slot)
- No team should referee two consecutive time slots
- Referee assignment happens after scheduling: for each game, the available team from the same group (not playing in that slot and not playing in the next slot) with fewest referee duties is assigned. This produces balanced distribution for any group size
- In round robin the team should have equal amount of home and away games if possible
- For playoffs, rounds must be scheduled in order (quarterfinals before semifinals before finals)

# Overview

- The following overviews of the tournament structure should be provided:
    - Phase/group overview
        - Accessible in a dedicated tab
        - Phases and groups should be collapsible
    - Court overview
        - Accessible from the Court tab
        - Accessible after selecting a court
    - Group overview
        - Accessible in a dedicated tab
        - Phases should be collapsible
        - Groups should be selectable
        - For each group, should present the standings of the group based on the game results
        - For each group, should present the list of games and the results
        - The expanded phases and selected groups (max one from each phase) should be a part of the URL query params

# Results

- It should be possible to enter the game results only using a tournament code
- It should be possible from the phase/group overview and from the court overview
- Game results should be visible in the overviews
- It should be possible to edit a previously recorded result using the same mechanism as entering it
- Results can be entered in two modes:
    - **Detailed** (default): per-set scores (e.g. 25-20, 23-25, 15-10); displayed in overviews when available
    - **Simple**: sets won only (e.g. 2-1); shown as a fallback when per-set data is not recorded
- Tie rules:
    - Simple results (sets-won mode): a tie is not allowed (one team must win more sets)
    - Detailed result with exactly 1 set: a tie is allowed (equal points permitted)
    - Detailed result with more than 1 set: a tie in set count is not allowed (one team must win more sets), and each individual set must also have a winner (no drawn sets)

# Standings

- Standings are calculated from completed game results for each group
- Standings calculation depends on the phase format:

## Round Robin
- 2 points for winning a game, 1 point for a draw, 0 points for losing
- Ordering: win points (descending), then set difference (descending), then point difference (descending), then direct result between the tied teams, then points scored (descending)
- Set difference = sets won minus sets lost (from HomeScore/AwayScore)
- Point difference = points won minus points lost (from set detail points, e.g. 25-20)
- Direct result tiebreaker: when teams are tied on points, set difference, and point difference, their head-to-head match result is used (applying the same tiebreaker chain within the h2h mini-table)
- Points scored = total points won (from set details, descending) — final tiebreaker after direct result
- Teams still tied after all tiebreakers share the same position

## Playoff Elimination
- No point system; positions are determined by the elimination round in which a team lost
- Position formula: `bracketSize / 2^round + 1` where round is 1-indexed
- Multiple teams share the same position (e.g., in an 8-team bracket: 4 QF losers all finish 5th, 2 SF losers finish 3rd)
- The winner of the final gets position 1, the runner-up gets position 2

## Playoff with Placement
- Each team gets a unique final position via placement games
- Placement games are single-game rounds at the end of the bracket (generated worst-to-best, Final last)
- The winner of the Final is 1st, loser is 2nd; winner of 3rd-place game is 3rd, loser is 4th; and so on
- Positions are only assigned for completed placement games

## Double Elimination (VD)
- Grand Final winner = 1st, Grand Final loser = 2nd
- Grand SF losers share 3rd position
- Crossover losers share 5th position
- 7th Place game winner = 7th, loser = 8th
- Teams that have not yet been eliminated default to the worst position

## Double Elimination
- Positions are determined by the Losers Bracket round in which a team was eliminated
- Grand Final winner = 1st, Grand Final loser = 2nd
- Teams eliminated in the same LB round share the same position (similar to Playoff Elimination)
- Earlier LB round losers get worse positions; later LB round losers get better positions
- Teams that have not yet been eliminated default to the worst position

## Final Standings (Tournament-wide)
- Final standings aggregate results across all phases to produce a single tournament-wide ranking
- Placeholder teams are excluded — only real teams appear
- The last phase (highest order) assigns positions 1 through N from its group standings
- For each earlier phase with progression config, teams that did **not** advance get positions starting after the advancing teams, ranked by their cross-group standings within that phase
    - Example: 8 teams in Phase 1, 4 advance to Phase 2 → Phase 2 standings give positions 1-4, Phase 1 non-advancing teams get positions 5-8
- When levels are used, final standings are calculated independently per level — each level has its own 1-N ranking
- Final standings are available as soon as any phase has completed games (provisional mode):
    - Completed phases contribute finalized positions for eliminated teams
    - The current (in-progress or last) phase contributes current positions for active teams
    - When not all phases are complete, standings are marked as "provisional"
- Final standings are read-only and publicly accessible (anonymous access)
- The response includes: position, team ID, team name, and optionally level name

# Bracket Advancement

- When a playoff game result is recorded (or corrected), downstream games referencing it via placeholders are automatically updated with actual team IDs
- "Winner {label}" placeholders resolve to the winning team; "Loser {label}" placeholders resolve to the losing team
- Each playoff game has a Label (e.g., "SF1", "QF2", "Final", "B-SF1", "WB-QF1", "LB-R1-1", "Grand Final") stored on the game entity and set during generation
- If a result is a draw (HomeScore == AwayScore), no advancement occurs — the organizer must correct the result
- Placeholders are kept intact after resolution (not cleared), allowing re-resolution when a result is corrected
- Round-robin games have no label and no advancement
- If a downstream game has already been played, its team IDs are updated but its result and status are preserved — the organizer is responsible for reviewing cascading changes

# Phase advancement

## Phase status

- Each phase has a status: `New` (default), `InProgress`, `Completed`
- **New → InProgress**: for the first phase, this happens when games are generated; for subsequent phases, when the previous phase completes (placeholder IDs are resolved to real team IDs)
- **InProgress → Completed**: owner explicitly marks the phase as complete via the Schedule tab
- Completing a phase requires all games in the phase to have results recorded
- Reopening a completed phase reverts it to `InProgress` and reverses placeholder resolution in the next phase (real IDs swapped back to placeholder IDs, next phase reverts to `New`)

## Progression

- Phase defines the amount of teams progressing via `GroupWinners` and/or `TotalTeamsProceeding`
- The progressing teams are the input to the next phase
- Progression is automatic when the phase is marked as completed:
    1. From each group, the top `GroupWinners` teams qualify automatically
    2. Remaining teams across all groups are ranked together using the same standings criteria (points → set difference → point difference)
    3. The best remaining teams are added until `TotalTeamsProceeding` is reached
    4. If only `GroupWinners` is set, total qualifying = `GroupWinners` × number of groups
    5. If only `TotalTeamsProceeding` is set, the top N teams across all groups qualify
    6. If neither is set, no progression occurs
    7. Setting `GroupWinners` and/or `TotalTeamsProceeding` to 0 explicitly marks the phase as final — no teams advance but the phase is recognized as having progression config for final standings calculation
- All qualifying teams are then ranked together in a single seeding order using standings criteria — this produces Seed 1, Seed 2, ..., Seed N
- Seeded teams are assigned to the next phase's groups via snake draft (same as existing auto-assign)

## Placeholder teams

- When a phase with order > 1 is created and the previous phase has progression config (`GroupWinners` and/or `TotalTeamsProceeding`), placeholder Team entities are automatically generated
- Placeholder count = `TotalTeamsProceeding` ?? `GroupWinners` × number of groups in the source phase
- Each placeholder is a real Team entity with `IsPlaceholder = true`, a `SourcePhaseId` (the phase whose progression config created them), and a `Seed` (1-based)
- Placeholder naming format: `"{SourcePhaseName} - Seed {N}"`
- Placeholders are fully functional: they can be assigned to groups (manually or via auto-assign), and games can be generated and scheduled using their IDs
- Auto-assign for phase 2+: placeholder teams ordered by seed, distributed via snake draft (same as real teams in phase 1). When placeholders are resolved (previous phase completed), auto-assign uses the resolved real team IDs instead of placeholder IDs
- When the previous phase's progression config changes (UpdatePhase), old placeholders are deleted and regenerated; any existing games in the next phase are also deleted
- When a phase is deleted, its associated placeholder teams (where `SourcePhaseId` matches any deleted phase) are also deleted

## Placeholder resolution

- When a phase is marked as completed, placeholder team IDs in the **next** phase are swapped to real team IDs based on the seeding order:
  - Seed 1 placeholder → best qualifying team, Seed 2 → second best, etc.
  - Swaps occur in: game `HomeTeamId`, `AwayTeamId`, `RefereeTeamId`, and group `TeamIds`
  - Each placeholder team's `ResolvedTeamId` is set to the real team it resolved to
- Reopening a completed phase reverses the swap: real team IDs are replaced back with placeholder team IDs, `ResolvedTeamId` is cleared, and the next phase reverts to `New`
- When placeholders are resolved (previous phase completed), structure editing for the next phase shows resolved real team names instead of placeholder names. Manual team assignment and auto-assign both operate on real team IDs
- Within-phase playoff placeholders (`HomeTeamPlaceholder` / `AwayTeamPlaceholder` strings like "Winner SF1") remain unchanged — these are separate from cross-phase placeholder teams

# Phase State Restrictions

Operations on phases, groups, teams, and courts are restricted based on phase status and game existence to protect data integrity:

## Completed phases (status = Completed)
- Cannot edit (update name, format, start time, progression config)
- Cannot delete
- Cannot add/rename/delete groups
- Cannot assign/remove teams
- Cannot auto-assign teams
- Cannot delete games
- Cannot record game results
- Must reopen first to make any changes

## Phases with generated games (InProgress or New with games)
- Cannot change structural fields (format, start time)
- Cannot add/delete groups
- Cannot assign/remove teams or auto-assign teams
- Cannot delete the phase
- **Can** change non-structural fields (name, groupWinners, totalTeamsProceeding)
- **Can** rename groups
- Must delete games first to modify structure

## Game deletion resets phase status
- When games are deleted from an InProgress phase, the phase status resets to New
- This ensures the phase is not left in an inconsistent state without games

## Reopen guard
- Cannot reopen a phase if the next phase has any completed games
- Must delete results in the next phase first

## Result recording guard
- Cannot record a result when either team is unassigned (null team ID, e.g. unresolved playoff placeholders)

## Referential integrity
- Cannot delete a team that is assigned to any group or referenced in any game
- Cannot delete a court that has scheduled games
- Must remove the references first (unassign team, delete games)

All state violations return HTTP 409 Conflict. Validation errors (e.g., unassigned teams) return HTTP 400.

# Levels

## Basic properties
- A phase can optionally define levels via `NumberOfLevels` (null or 0 means no levels)
- When levels are defined, groups are evenly distributed across levels
- `NumberOfGroups` represents groups **per level** (total groups = NumberOfGroups × NumberOfLevels)
- Each level has a default name ("Level 1", "Level 2") that can be customized
- Level names must be unique within a phase
- Levels have an order (1-based) determining their ranking priority
- When levels are not defined, everything behaves exactly as before (backward compatible)
- Levels are structural — the number of levels cannot be changed while games exist
- Level names can be updated independently of other phase properties
- Levels cannot be modified on a completed phase

## Cascading level constraint
- Levels cascade forward through phases: once a phase defines levels, subsequent phases must maintain at least as many levels
- Specifically, Phase N+1's `NumberOfLevels` must be a **multiple** of Phase N's level count (e.g., 2→2, 2→4, 2→6 are valid; 2→3, 2→1 are not)
- If Phase N has no levels, Phase N+1 can freely introduce levels or have none — levels can be introduced at any point
- Validation is applied when adding a new phase and prevents invalid level counts with a 400 error
- When deleting a phase, re-validate that the gap doesn't break the constraint between the now-adjacent phases

## Auto-assign with levels
- Auto-assign respects levels: teams are split by seed into levels (top half → Level 1, bottom half → Level 2), then snake-drafted within each level's groups
- Manual team assignment has no level restrictions — teams can be freely assigned to any group regardless of level

## Progression with levels
- At phase completion, rankings incorporate levels (all Level 1 teams ranked above Level 2)
- When levels are defined, `GroupWinners` and `TotalTeamsProceeding` apply per level. The total number of teams advancing is multiplied by the number of levels

## Level-scoped progression (cross-phase)
- When levels increase between phases (e.g., source phase has 2 levels, target phase has 4), progression uses **level-scoped split**
- The split factor is `targetLevels / sourceLevels` (e.g., 4 / 2 = 2)
- Qualifying teams from source Level K feed into target levels `(K-1) × splitFactor + 1` through `K × splitFactor`
  - Example: source Level 1 → target Levels 1-2; source Level 2 → target Levels 3-4
- Within each target level, teams are seeded by their qualifying rank and distributed via snake draft
- When the source phase has no levels but the target has levels, all qualifying teams are treated as a single pool and distributed across target levels by seed (top seeds → Level 1, etc.)
- When both phases have the same number of levels, progression works as before (Level 1 → Level 1, Level 2 → Level 2)

## Placeholder generation with level split
- Placeholder generation respects the level-scoped split: when levels increase, placeholders are distributed across target levels proportionally
- Each source level's expected team count is split evenly across its corresponding target levels
- Placeholder naming includes the source level context: `"{SourcePhaseName} - {SourceLevelName} - Seed {N}"`

# TBC

# Phase

- Phases structure can be copied from another tournament

