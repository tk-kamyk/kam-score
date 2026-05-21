# Game Generation — design

Paired with [../requirements/game-generation.md](../requirements/game-generation.md).

## Per-format game construction

Supports [FR-GAM-004], [FR-GAM-005], [FR-GAM-006], [FR-GAM-007].

### Round Robin

All-play-all within each group. Pairings use the circle method with home/away balance.

### Playoff Elimination

Single-elimination bracket per group. Round 1 uses real seeded team IDs; later rounds use placeholders (e.g. `"Winner SF1"`, `"Winner QF2"`).

For non-power-of-two team counts, the bracket is padded to the next power of two and unused slots become byes. Round-1 pair groupings are then reordered before round 2 is generated so that pairs with both slots from byes come first, pairs with both slots from real round-1 games come next, and pairs mixing a bye with a round-1 game come last. The result puts the team that just played round 1 into the latest match of the next round.

### Playoff with Placement

Single-elimination bracket with placement games for all final positions (1st through Nth). After each elimination round, losers form a consolation bracket (B) and winners form the main bracket (A). Consolation rounds are scheduled before main-bracket rounds at each level. Placement games are ordered worst-to-best position, with the **Final always last**.

Example for 8 teams: `QF → B-SF (QF losers) → A-SF (QF winners) → 7th → 5th → 3rd → Final`.

### Double Elimination (VD) — volleyball variant, exactly 8 teams per group

Structure (14 games, 7 rounds):
- 4 QFs (seeded `1v8`, `4v5`, `2v7`, `3v6`)
- 2 QF Winners games and 2 QF Losers games
- 2 Crossover games (cross-bracket: `Loser W2 vs Winner L1`, `Loser W1 vs Winner L2`)
- 2 Grand SFs (same-half: `Winner W1 vs Winner X1`, `Winner W2 vs Winner X2`)
- 7th Place game (two 0-2 teams) and Grand Final

Positions: 1st/2nd from Grand Final; 3rd-4th shared (Grand SF losers); 5th-6th shared (Crossover losers); 7th/8th from 7th Place game. The layout is only generated when the group has exactly 8 teams; other counts fall back to the standard Double Elimination layout.

### Double Elimination (standard)

Teams must lose twice to be eliminated. Structure: Winners Bracket (WB), Losers Bracket (LB), Grand Final. WB is standard single-elimination; losers drop to LB. LB alternates between rounds where WB losers enter (drop-down) and rounds where LB teams play each other. Grand Final is a single game between WB winner and LB winner (no reset match).

Example for 8 teams: `WB-QF (4) → WB-SF (2) → WB-Final (1); LB-R1 (2, QF losers) → LB-R2 (2, LB-R1 winners vs SF losers) → LB-R3 (1) → LB-R4 (1, vs WB-Final loser); Grand Final`.

## [FR-GAM-027] Referee eligibility

For each game, walk all teams in scope (same group, or same level if the phase has levels). A team is eligible only if: (1) not playing in the game's time slot, (2) not refereeing in the game's time slot, (3) not playing in the next time slot.

For elimination phases, bracket placeholders from earlier rounds in the same group are also candidates and follow the same rules. The auto-assigner picks the eligible team with the fewest referee duties so far (tie-broken deterministically by team order).
