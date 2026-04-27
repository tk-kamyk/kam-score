# Results and Standings

> See design: [../design/results-and-standings.md](../design/results-and-standings.md) for tiebreaker algorithms, position formulas, and progression-highlighting math.

## Recording Results

- Game results can be entered using a tournament code (participant access) or by the owner
- Results can be entered and edited from the phase/group overview and from the court overview
- Game results are visible in all overviews
- Editing a previously recorded result uses the same mechanism as entering it
- Two entry modes:
    - **Detailed** (default): per-set scores (e.g. 25-20, 23-25, 15-10); displayed in overviews when available
    - **Simple**: sets won only (e.g. 2-1); shown as a fallback when per-set data is not recorded

### Tie rules
- Simple results (sets-won mode): a tie is not allowed
- Detailed result with exactly 1 set: a tie is allowed (equal points permitted)
- Detailed result with more than 1 set: a tie in set count is not allowed, and each individual set must also have a winner (no drawn sets)

## Standings

- Standings are calculated from completed game results for each group
- Standings calculation depends on the phase format

### Round Robin
- 2 points for winning a game, 1 point for a draw, 0 points for losing
- Ordering cascade: win points → set difference → point difference → direct result between tied teams → points scored
- Teams still tied after all tiebreakers share the same position

### Playoff Elimination
- No point system; positions are determined by the elimination round in which a team lost
- Multiple teams can share the same position (e.g., in an 8-team bracket: 4 QF losers all finish 5th)
- Winner of the final gets position 1, runner-up gets position 2

### Playoff with Placement
- Each team gets a unique final position via placement games
- Positions are only assigned for completed placement games

### Double Elimination (VD) and Double Elimination
- Positions are derived from the round in which a team was eliminated
- Grand Final winner = 1st, loser = 2nd
- Teams not yet eliminated default to the worst position

### Custom (manual standings)
- Standings are entered by the owner as an ordered team list per group (1st, 2nd, 3rd …); position is derived from order
- Per-team stats (wins, points, set difference, etc.) are not entered and are displayed as blank
- Entry is only allowed when the owner is authenticated **and** the phase is `InProgress`; reopening a completed Custom phase returns it to `InProgress` so edits resume
- Saving standings for a group fully replaces the previous ordering for that group; each group is saved independently
- An order is only valid if it lists every team currently assigned to the group exactly once (no duplicates, no foreign IDs, no partial orderings)
- If the group's team list changes (team added or removed), any previously saved ordering for that group is cleared
- Cross-group ranking (used for "best remaining" and seeding) orders teams by their manually entered position; ties across groups are resolved by group order

## Progression Highlighting

- When a phase has progression config (`groupWinners` and/or `totalTeamsProceeding`), standings rows are visually highlighted to indicate qualification status
- Applies to all phase formats
- **Direct qualifiers**: position ≤ `groupWinners` → green/success
- **Candidates**: teams that could qualify via cross-group "best remaining" ranking → yellow/warning
- When only `groupWinners` is set: only direct qualifiers are highlighted
- When only `totalTeamsProceeding` is set: top N positions per group are highlighted as candidates
- When neither is set: no highlighting
- With levels: highlighting scope is the level, not the whole phase

## Bracket Advancement

- When a playoff game result is recorded (or corrected), downstream games referencing it via placeholders are automatically updated with actual team IDs
- `"Winner {label}"` placeholders resolve to the winning team; `"Loser {label}"` placeholders resolve to the losing team
- Each playoff game has a `Label` (e.g., `"SF1"`, `"Final"`, `"WB-QF1"`, `"Grand Final"`) stored on the game and set during generation
- If a result is a draw (HomeScore == AwayScore), no advancement occurs — the organizer must correct the result
- Placeholders are kept intact after resolution, allowing re-resolution when a result is corrected
- Round-robin games have no label and do not trigger advancement
- If a downstream game has already been played, its team IDs are updated but its result and status are preserved — the organizer is responsible for reviewing cascading changes

## Final Standings (Tournament-wide)

- Final standings show the standings of the **last phase** only
- Only available when the last phase has status `Completed` — returns empty list otherwise
- Placeholder teams are excluded
- Teams from earlier phases that did not advance are **not** included
- When the last phase has levels, standings are calculated independently per level
- Final standings are read-only and publicly accessible (anonymous access)
- The response includes: position, team ID, team name, and optionally level name
