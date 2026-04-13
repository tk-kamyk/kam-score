# Results and Standings — Design Details

Paired with [../requirements/results-and-standings.md](../requirements/results-and-standings.md).

## Round-robin tiebreaker chain

1. **Win points** (2 win / 1 draw / 0 loss), descending
2. **Set difference** = sets won − sets lost (from `HomeScore`/`AwayScore`), descending
3. **Point difference** = points won − points lost (from per-set detail), descending
4. **Direct result between tied teams**: when teams are tied on the above, their head-to-head mini-table is recomputed using the same cascade
5. **Points scored** = total points won (from set details), descending

Teams still tied after all five criteria share the same position.

## Playoff Elimination — position formula

```
position = bracketSize / 2^round + 1      (round is 1-indexed)
```

Example for 8-team bracket:
- Final losers (round 3): `8 / 8 + 1 = 2`
- SF losers (round 2): `8 / 4 + 1 = 3` (shared)
- QF losers (round 1): `8 / 2 + 1 = 5` (shared)

## Playoff with Placement

Placement games are single-game rounds at the end of the bracket, generated worst-to-best, Final last:

- Winner of the Final → 1st, loser → 2nd
- Winner of the 3rd-place game → 3rd, loser → 4th
- Winner of the 5th-place game → 5th, loser → 6th
- …and so on

## Double Elimination (VD)

| Position | Source |
|----------|--------|
| 1st | Grand Final winner |
| 2nd | Grand Final loser |
| 3rd (shared) | Grand SF losers |
| 5th (shared) | Crossover losers |
| 7th | 7th Place game winner |
| 8th | 7th Place game loser |

Teams not yet eliminated default to the worst position.

## Double Elimination (standard)

- Positions mirror the Playoff Elimination model applied to the Losers Bracket
- Teams eliminated in the same LB round share the same position
- Grand Final winner = 1st, loser = 2nd

## Progression Highlighting math

Let `groupsInScope` = number of groups in the level (or phase if no levels).

```
wildcardSlots = totalTeamsProceeding - groupWinners × groupsInScope
candidateDepth = ceil(wildcardSlots / groupsInScope)
```

- Rows at position ≤ `groupWinners` → **direct qualifiers** (green)
- Rows at position in `(groupWinners, groupWinners + candidateDepth]` → **candidates** (yellow)
- When only `totalTeamsProceeding` is set: `candidateDepth = ceil(totalTeamsProceeding / groupsInScope)` starting from position 1
- When only `groupWinners` is set: no candidates, only direct qualifiers
- When neither is set: no highlighting
