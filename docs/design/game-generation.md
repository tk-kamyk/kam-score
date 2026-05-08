# Game Generation — Design Details

Paired with [../requirements/game-generation.md](../requirements/game-generation.md). This file captures implementation-level specifics that should **not** appear as user-facing requirements but must be preserved as a spec for engineers.

## Per-format game construction

### Round Robin
- All-play-all within each group
- Uses circle method for pairings with home/away balance

### Playoff Elimination
- Single-elimination bracket per group
- First round uses real team IDs (seeded)
- Later rounds use placeholders (e.g., `"Winner SF1"`, `"Winner QF2"`)
- Non-power-of-two team counts: the bracket is padded to the next power of two and unused slots become byes. Pair groupings of round 1 are then reordered before round 2 is generated so that pairs with both slots from byes come first, pairs with both slots from real round-1 games come next, and pairs mixing a bye with a round-1 game come last. This puts the team that just played round 1 into the latest match of the next round

### Playoff with Placement
- Single-elimination bracket with full placement games for all final positions (1st through Nth)
- After each elimination round: losers form a consolation bracket (B), winners form the main bracket (A)
- Consolation rounds are always scheduled **before** main bracket rounds at each level
- Placement games (final position matches) are ordered worst-to-best position, with the **Final always last**
- Example for 8 teams: `QF → B-SF (QF losers) → A-SF (QF winners) → 7th → 5th → 3rd → Final`

### Double Elimination (VD) — volleyball variant, exactly 8 teams per group
- Structure:
  - 4 QFs (seeded `1v8`, `4v5`, `2v7`, `3v6`)
  - 2 QF Winners games
  - 2 QF Losers games
  - 2 Crossover games (cross-bracket: `Loser W2 vs Winner L1`, `Loser W1 vs Winner L2`)
  - 2 Grand SFs (same-half: `Winner W1 vs Winner X1`, `Winner W2 vs Winner X2`)
  - 7th Place game (two 0-2 teams)
  - Grand Final
- Total: **14 games, 7 rounds**
- Positions:
  - 1st / 2nd from Grand Final
  - 3rd-4th shared (Grand SF losers)
  - 5th-6th shared (Crossover losers)
  - 7th / 8th from 7th Place game
- Validation: only generates the VD layout when the group has exactly 8 teams; for any other count the strategy delegates to the standard Double Elimination strategy

### Double Elimination (standard)
- Teams must lose twice to be eliminated
- Structure: Winners Bracket (WB), Losers Bracket (LB), Grand Final
- WB is standard single-elimination; losers drop to LB
- LB alternates between rounds where WB losers enter (drop-down) and rounds where LB teams play each other
- Grand Final: single game between WB winner and LB winner (no reset match)
- Example for 8 teams:
  - `WB-QF (4 games) → WB-SF (2 games) → WB-Final (1 game)`
  - `LB-R1 (2 games, QF losers) → LB-R2 (2 games, LB-R1 winners vs SF losers) → LB-R3 (1 game) → LB-R4 (1 game, vs WB-Final loser)`
  - `Grand Final`

## Referee eligibility algorithm

For each game, walk all teams in scope (same group, or same level if phase has levels). A team is eligible only if:
1. Not playing in game's time slot
2. Not refereeing in game's time slot
3. Not playing in the **next** time slot

For elimination phases, bracket placeholders from earlier rounds in the same group are also candidates. Placeholders follow the same eligibility rules.

Auto-assigner picks the eligible team with fewest referee duties so far (tie-broken deterministically by team order).
