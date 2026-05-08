# Game Generation

> See design: [../design/game-generation.md](../design/game-generation.md) for per-format bracket sequences and scheduling algorithm details.

## Game

- Game is defined by:
    - the home team (or placeholder for playoff)
    - the away team (or placeholder for playoff)
    - the refereeing team
    - the court
    - the start time
    - the result (optional — recorded after the game is played)
- Games should not be created manually
- A single "Generate & Schedule" button generates all games in a phase and auto-schedules them across courts and time slots
- Game generation produces different game sets depending on the phase format (round-robin, playoff-elimination, playoff-with-placement, double-elimination, double-elimination-vd)
- Playoffs and double elimination apply to each group individually; bracket size is determined by the number of teams in the group
- All formats must produce a valid schedule for any number of teams in a group, including non-power-of-two counts.
- Double Elimination (VD) is used only when the group contains exactly 8 teams; for any other team count the phase falls back to the standard Double Elimination format
- Scheduling uses the tournament-level game length (minutes) for time slot duration and the phase-level start time as the baseline

## Schedule

- The games should be uniformly distributed among courts
- The games within each phase should be uniformly distributed among groups (e.g. the first game should happen at the same time in each group if possible due to amount of courts)
- No team should play or referee in two games at the same time
- A team must have a free time slot before any game they play in (no activity — playing or refereeing — in the immediately preceding slot)
- No team should referee two consecutive time slots
- Referee assignment happens after scheduling: for each game, the available team from the same group (not playing in that slot and not playing in the next slot) with fewest referee duties is assigned. This produces balanced distribution for any group size
- In round robin the team should have equal amount of home and away games if possible
- For playoffs, rounds must be scheduled in order (quarterfinals before semifinals before finals)

## Manual Referee Assignment

- The tournament owner can manually assign a referee to any game that currently has no referee assigned
- A button to assign a referee is shown in the Schedule tab and the Court view, only for the owner
- Clicking the button opens a dialog listing candidate teams
- Candidate teams are all teams from the same level within the phase (across all groups in that level). If the phase has no levels, all teams in the phase are candidates
- In elimination phases, the candidate list includes bracket placeholders from all earlier rounds in the same group. The same eligibility rules apply to placeholders
- A team is eligible ("free") only if it is not playing and not refereeing in the game's time slot, and not playing in the next time slot
- Owner-only action (JWT authentication required)
- The API provides both a candidate list endpoint and an assignment endpoint
- After assignment, the referee name (or placeholder label) is displayed in the schedule
- When a game result resolves bracket advancement, any referee placeholder referencing that game also resolves to the real team ID
