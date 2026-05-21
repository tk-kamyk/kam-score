# Results and Standings

> See design: [../design/results-and-standings.md](../design/results-and-standings.md) for tiebreaker cascades, position formulas, and progression-highlighting math.

## Recording results

- [FR-RES-001] Game results can be entered using a tournament code (participant access) or by the owner.
- [FR-RES-002] Results can be entered and edited from the phase/group overview and from the court overview.
- [FR-RES-003] Game results are visible in all overviews.
- [FR-RES-004] Editing a previously recorded result uses the same mechanism as entering it.
- [FR-RES-005] Two entry modes are supported: **Detailed** (default) — per-set scores, displayed in overviews when available; **Simple** — sets won only, shown as a fallback when per-set data is not recorded.

### Tie rules

- [FR-RES-010] Simple results (sets-won mode): a tie is not allowed.
- [FR-RES-011] Detailed result with exactly 1 set: a tie is allowed (equal points permitted).
- [FR-RES-012] Detailed result with more than 1 set: a tie in set count is not allowed, and each individual set must also have a winner (no drawn sets).

## Standings

- [FR-RES-020] Standings are calculated from completed game results for each group; the calculation depends on the phase format.

### Round Robin

- [FR-RES-030] 2 points for winning a game, 1 point for a draw, 0 for losing.
- [FR-RES-031] Teams are ordered by a tiebreaker cascade defined in the design doc; teams still tied after the full cascade share the same position.

### Playoff Elimination

- [FR-RES-040] No point system; positions are determined by the elimination round in which a team lost.
- [FR-RES-041] Multiple teams can share the same position (e.g. in an 8-team bracket, the four QF losers all finish 5th).
- [FR-RES-042] Winner of the final gets position 1, runner-up gets position 2.

### Playoff with Placement

- [FR-RES-050] Each team gets a unique final position via placement games.
- [FR-RES-051] Positions are only assigned for completed placement games.

### Double Elimination (VD) and Double Elimination

- [FR-RES-060] Positions are derived from the round in which a team was eliminated; Grand Final winner = 1st, loser = 2nd.
- [FR-RES-061] Teams not yet eliminated default to the worst position.

### Custom (manual standings)

- [FR-RES-070] Standings are entered by the owner as an ordered team list per group (1st, 2nd, 3rd …); position is derived from order.
- [FR-RES-071] Per-team stats (wins, points, set difference, etc.) are not entered and are displayed as blank.
- [FR-RES-072] Entry is only allowed when the owner is authenticated and the phase is `InProgress`; reopening a completed Custom phase returns it to `InProgress` so edits resume.
- [FR-RES-073] Saving standings for a group fully replaces the previous ordering for that group; each group is saved independently.
- [FR-RES-074] An order is only valid if it lists every team currently assigned to the group exactly once (no duplicates, no foreign IDs, no partial orderings).
- [FR-RES-075] If the group's team list changes (team added or removed), any previously saved ordering for that group is cleared.
- [FR-RES-076] Cross-group ranking (used for "best remaining" and seeding) orders teams by their manually entered position; ties across groups are resolved by group order.

## Progression highlighting

- [FR-RES-080] When a phase has progression config (`GroupWinners` and/or `TotalTeamsProceeding`), standings rows are visually highlighted to indicate qualification status; applies to all phase formats.
- [FR-RES-081] **Direct qualifiers**: position ≤ `GroupWinners` → highlighted as qualified (green/success).
- [FR-RES-082] **Candidates**: teams that could qualify via cross-group "best remaining" ranking → highlighted as candidate (yellow/warning).
- [FR-RES-083] When only `GroupWinners` is set, only direct qualifiers are highlighted.
- [FR-RES-084] When only `TotalTeamsProceeding` is set, top N positions per group are highlighted as candidates.
- [FR-RES-085] When neither is set, no highlighting is shown.
- [FR-RES-086] With levels, highlighting scope is the level, not the whole phase.

## Bracket advancement

- [FR-RES-090] When a playoff game result is recorded (or corrected), downstream games referencing it via placeholders are automatically updated with the actual team IDs.
- [FR-RES-091] `"Winner {label}"` placeholders resolve to the winning team; `"Loser {label}"` placeholders resolve to the losing team.
- [FR-RES-092] Each playoff game has a `Label` (e.g. `"SF1"`, `"Final"`, `"WB-QF1"`, `"Grand Final"`) stored on the game and set during generation.
- [FR-RES-093] If a result is a draw (HomeScore == AwayScore), no advancement occurs — the organiser must correct the result.
- [FR-RES-094] Placeholders are kept intact after resolution so re-resolution is possible when a result is corrected.
- [FR-RES-095] Round-robin games have no label and do not trigger advancement.
- [FR-RES-096] If a downstream game has already been played, its team IDs are updated but its result and status are preserved — the organiser is responsible for reviewing cascading changes.

## Final standings (tournament-wide)

- [FR-RES-110] Final standings show the standings of the last phase only.
- [FR-RES-111] Final standings are only available when the last phase has status `Completed`; otherwise the response is an empty list.
- [FR-RES-112] Placeholder teams are excluded; teams from earlier phases that did not advance are not included.
- [FR-RES-113] When the last phase has levels, standings are calculated independently per level.
- [FR-RES-114] Final standings are read-only and publicly accessible (anonymous access).
- [FR-RES-115] The response includes position, team ID, team name, and optionally level name.
