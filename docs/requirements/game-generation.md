# Game Generation

> See design: [../design/game-generation.md](../design/game-generation.md) for per-format bracket sequences and scheduling algorithm details.

## Game

- [FR-GAM-001] A game is defined by home team (or placeholder for playoff), away team (or placeholder), refereeing team, court, start time, and an optional result recorded after play.
- [FR-GAM-002] Games are never created manually.
- [FR-GAM-003] A single "Generate & Schedule" action generates all games in a phase and auto-schedules them across courts and time slots.
- [FR-GAM-004] Game generation produces different game sets depending on the phase format (round-robin, playoff-elimination, playoff-with-placement, double-elimination, double-elimination-vd).
- [FR-GAM-005] Playoffs and double elimination apply to each group individually; bracket size is determined by the number of teams in the group.
- [FR-GAM-006] All formats must produce a valid schedule for any number of teams in a group, including non-power-of-two counts.
- [FR-GAM-007] Double Elimination (VD) is used only when the group contains exactly 8 teams; for any other count the phase falls back to the standard Double Elimination format.
- [FR-GAM-008] Scheduling uses the tournament-level game length (minutes) for time slot duration and the phase-level start time as the baseline.

## Schedule

- [FR-GAM-020] Games are uniformly distributed among courts.
- [FR-GAM-021] Games within each phase are uniformly distributed among groups (e.g. the first game starts at the same time in each group when court count allows).
- [FR-GAM-022] No team plays or referees in two games at the same time.
- [FR-GAM-023] A team must have a free time slot before any game they play in (no playing or refereeing in the immediately preceding slot).
- [FR-GAM-024] No team referees two consecutive time slots.
- [FR-GAM-025] In round robin, a team has an equal number of home and away games when possible.
- [FR-GAM-026] For playoffs, rounds are scheduled in order (quarterfinals before semifinals before finals).
- [FR-GAM-027] Referee assignment happens after scheduling: for each game, the eligible team from the same group with fewest referee duties is assigned; this produces balanced distribution for any group size.

## Manual referee assignment

- [FR-GAM-040] The tournament owner can manually assign a referee to any game that currently has no referee assigned; only the owner (JWT) can do this.
- [FR-GAM-041] An "assign referee" button is shown in the Schedule tab and the Court view to the owner; clicking it opens a dialog listing candidate teams.
- [FR-GAM-042] Candidate teams are all teams from the same level within the phase (across all groups in that level). If the phase has no levels, all teams in the phase are candidates.
- [FR-GAM-043] In elimination phases, the candidate list includes bracket placeholders from all earlier rounds in the same group; the same eligibility rules apply to placeholders.
- [FR-GAM-044] A team is eligible only if it is not playing and not refereeing in the game's time slot, and not playing in the next time slot.
- [FR-GAM-045] After assignment, the referee name (or placeholder label) is displayed in the schedule.
- [FR-GAM-046] When a game result resolves bracket advancement, any referee placeholder referencing that game also resolves to the real team ID.
- [FR-GAM-047] The API provides both a candidate-list endpoint and an assignment endpoint.
