# Structure

- [FR-STR-001] A tournament has one structure, initialized explicitly by the owner.
- [FR-STR-002] The structure contains an ordered list of phases.
- [FR-STR-003] Structure overview is available to everyone; editing requires authentication.

## Related documents

- [game-generation.md](./game-generation.md) — game creation, scheduling, referee assignment
- [results-and-standings.md](./results-and-standings.md) — recording results, standings, bracket advancement
- [phase-advancement.md](./phase-advancement.md) — phase status, progression, placeholder teams
- [phase-state-restrictions.md](./phase-state-restrictions.md) — restriction matrix
- [levels.md](./levels.md) — per-phase levels

## Phase

- [FR-STR-010] A phase is represented by:
    - name
    - game format (round robin, play-off elimination, play-off with placement, double elimination, custom)
    - number of groups (auto-named A, B, C…)
    - group winners (optional) — how many teams per group qualify automatically to the next phase
    - total teams proceeding (optional) — total qualifying from this phase (combined ranking: group winners on top, then remaining teams, cutoff at total)
    - start time (optional, HH:mm) — baseline for scheduling games in this phase
- [FR-STR-011] Phases are ordered sequentially and automatically reordered when one is deleted.
- [FR-STR-012] Teams are assigned to groups via auto-assign (snake draft on team level for the first phase, random for later phases) or manually.
- [FR-STR-013] Manual assignment can override auto-assign; retriggering auto-assign resets manual edits.
- [FR-STR-014] In each phase, every post-game position is assigned a unique identifier (e.g. `phaseA-groupA-position1`).
- [FR-STR-015] Output positions from one phase feed as seeded teams into the next.
- [FR-STR-016] Phase edit is restricted to authenticated users; the saved phase overview is public.

### Custom format

- [FR-STR-020] A `Custom` phase generates no games — no scheduling, no recorded games against the phase.
- [FR-STR-021] When `Custom` is selected the form shows: "No games will be created for this phase. Once all teams are assigned, you'll be able to enter standings manually for each group."
- [FR-STR-022] A `Custom` phase is started via the same action used for other formats (labelled "Start phase" instead of "Generate games"); there is no `Scheduled` intermediate state.
- [FR-STR-023] The owner enters final standings per group manually; progression uses those positions (see [results-and-standings.md](./results-and-standings.md) and [phase-advancement.md](./phase-advancement.md)).
- [FR-STR-024] Number of groups, levels, `GroupWinners`, `TotalTeamsProceeding`, and seeding behave identically to other formats.

## Group

- [FR-STR-030] A group is represented by name, teams, games, and standings.
- [FR-STR-031] Group name is unique within a phase.
- [FR-STR-032] Teams can be assigned to and removed from groups individually.
- [FR-STR-033] A team cannot be assigned to two groups in the same phase.

## Overview

- [FR-STR-040] The system provides a **phase/group overview** in a dedicated tab; phases and groups are collapsible.
- [FR-STR-041] The system provides a **court overview** from the Court tab, after selecting a court.
- [FR-STR-042] The system provides a **group overview** in a dedicated tab where phases are collapsible and groups are selectable; for each group it presents the standings (based on game results) and the list of games with results.
- [FR-STR-043] Expanded phases and selected groups (at most one per phase) are persisted in the URL query params.
