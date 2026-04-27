# Structure

- A tournament has one structure, initialized explicitly by the owner
- The structure contains an ordered list of phases
- Structure overview is available to everyone; editing requires authentication

## Related documents

- [game-generation.md](./game-generation.md) — game creation, scheduling, referee assignment
- [results-and-standings.md](./results-and-standings.md) — recording results, standings, bracket advancement
- [phase-advancement.md](./phase-advancement.md) — phase status, progression, placeholder teams
- [phase-state-restrictions.md](./phase-state-restrictions.md) — restriction matrix for editing phases, groups, teams, courts
- [levels.md](./levels.md) — per-phase levels

# Phase

- Phase defines a part of tournament
- Phase is represented by:
    - name
    - format of the games (round robin, play-off elimination, play-off with placement games, double elimination, custom)
    - number of groups (specified on creation, auto-named A, B, C...)
    - group winners (optional) — how many teams per group qualify automatically to the next phase
    - total teams proceeding (optional) — total number of teams qualifying from this phase (a combined ranking is built: group winners on top, then remaining teams, cutoff applied at total)
    - start time (optional) — baseline time for scheduling games in this phase (HH:mm format)
- Phases are ordered sequentially (1, 2, 3...) and automatically reordered when one is deleted
- Teams are assigned to groups via auto-assign (snake draft based on team level for first phase, random for later phases) or manually
- Manual assignment can override auto-assign; retriggering auto-assign resets manual edits
- **Custom format** — for phases where games are played outside the system:
    - No games are generated, scheduled, or recorded against the phase
    - When `Custom` is selected in the phase form, an information message is shown: "No games will be created for this phase. Once all teams are assigned, you'll be able to enter standings manually for each group."
    - The phase is started from the same action used for other formats (labelled "Start phase" instead of "Generate games"); there is no `Scheduled` intermediate state arising from game generation
    - The owner enters the final standings per group manually; progression to the next phase uses those manually entered positions (see [results-and-standings.md](./results-and-standings.md) and [phase-advancement.md](./phase-advancement.md))
    - Number of groups, levels, `GroupWinners`, `TotalTeamsProceeding`, and seeding behave identically to other formats
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
