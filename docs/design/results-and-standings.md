# Results and Standings — design

Paired with [../requirements/results-and-standings.md](../requirements/results-and-standings.md).

## [FR-RES-031] Round-robin tiebreaker cascade

Teams are ordered by:

1. **Win points** (2 win / 1 draw / 0 loss), descending.
2. **Set difference** (sets won − sets lost), descending.
3. **Point difference** (points won − points lost from per-set detail), descending.
4. **Direct result between tied teams**: their head-to-head mini-table is recomputed using the same cascade.
5. **Points scored** (total points won), descending.

Teams still tied after all five criteria share the same position.

## [FR-RES-040] Playoff Elimination — position formula

```
position = bracketSize / 2^round + 1      (round is 1-indexed)
```

Example for 8-team bracket: Final losers (round 3) → 2; SF losers (round 2) → 3 (shared); QF losers (round 1) → 5 (shared).

## [FR-RES-050] Playoff with Placement — ordering

Placement games are single-game rounds at the end of the bracket, generated worst-to-best with the Final last. Winner of each placement game takes the odd position, loser takes the even position.

## [FR-RES-060] Double Elimination (VD) — positions

| Position | Source |
|----------|--------|
| 1st | Grand Final winner |
| 2nd | Grand Final loser |
| 3rd (shared) | Grand SF losers |
| 5th (shared) | Crossover losers |
| 7th | 7th Place game winner |
| 8th | 7th Place game loser |

Teams not yet eliminated default to the worst position.

## [FR-RES-060] Double Elimination (standard) — positions

Positions mirror the Playoff Elimination model applied to the Losers Bracket. Teams eliminated in the same LB round share the same position. Grand Final winner = 1st, loser = 2nd.

## [FR-RES-070] Custom (manual standings)

**Storage** — each group keeps an ordered list of team IDs as its manual standings; index `i` corresponds to position `i + 1`. The list is authoritative; per-team stats fields (points, wins, set difference, …) are projected as blank.

**Ranking** — for a group, standings are emitted by walking the ordered list. Cross-group ranking is a stable sort by position ascending; ties across groups (e.g. every group's 1st-place team) are resolved by the order in which the orderings were supplied. This deliberately overrides the games-based default (which would tie every team at zero on points).

**Invariants** — count equals the group's team count, no duplicates, every listed ID is currently in the group. Violations are rejected with HTTP 400.

**Cascading clears** — adding, removing, replacing, or clearing the team set of a group also clears its manual ordering, so a stale ordering can never reference a team that is no longer in the group. Changing a phase's format clears the orderings of every group in that phase.

**Final standings** — the final-standings endpoint filters out placeholder teams before invoking the format strategy, so the same code path works for all formats including Custom.

## [FR-RES-080] Progression highlighting math

Let `groupsInScope` = number of groups in the level (or phase if no levels).

```
wildcardSlots = totalTeamsProceeding - groupWinners × groupsInScope
candidateDepth = ceil(wildcardSlots / groupsInScope)
```

- Rows at position ≤ `groupWinners` → **direct qualifiers** (green).
- Rows at position in `(groupWinners, groupWinners + candidateDepth]` → **candidates** (yellow).
- When only `totalTeamsProceeding` is set: `candidateDepth = ceil(totalTeamsProceeding / groupsInScope)` starting from position 1.
- When only `groupWinners` is set: no candidates, only direct qualifiers.
- When neither is set: no highlighting.
