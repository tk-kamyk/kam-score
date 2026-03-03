# Structure

- A tournament has one structure, initialized explicitly by the owner
- The structure contains an ordered list of phases
- Structure overview is available to everyone; editing requires authentication

# Phase

- Phase defines a part of tournament
- Phase is represented by:
    - name
    - format of the games (round robin, play-off elimination, play-off with placement games)
    - number of groups (specified on creation, auto-named A, B, C...)
    - group winners (optional) — how many teams per group qualify automatically to the next phase
    - total teams proceeding (optional) — total number of teams qualifying from this phase (a combined ranking is built: group winners on top, then remaining teams, cutoff applied at total)
    - start time (optional) — baseline time for scheduling games in this phase (HH:mm format)
- Phases are ordered sequentially (1, 2, 3...) and automatically reordered when one is deleted
- Teams are assigned to groups via auto-assign (snake draft based on team level for first phase, random for later phases) or manually
- Manual assignment can override auto-assign; retriggering auto-assign resets manual edits
- In each phase, each position after the game is played is assigned a unique identifier, e.g. phaseA-groupA-position1 (TBC)
- Output from one phase (positions after the games) is input (seeded teams) to another phase (TBC)
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

# Game

- Game is defined by
    - the home team (or placeholder for playoff)
    - the away team (or placeholder for playoff)
    - the refereeing team
    - the court
    - the start time
    - the result (optional — recorded after the game is played)
- Games should not be created manually
- A single "Generate & Schedule" button generates all games in a phase and auto-schedules them across courts and time slots
- Game generation depends on the phase format:
    - **Round Robin**: all-play-all within each group. Uses circle method for pairings with home/away balance. Referee auto-assigned for every game: the available team (not playing) with fewest referee duties so far. This produces balanced distribution for any group size
    - **Playoff Elimination**: single-elimination bracket per group. First round uses real team IDs (seeded). Later rounds use placeholders (e.g., "Winner SF1", "Winner QF2")
    - **Playoff with Placement**: single-elimination bracket with full placement games for all final positions (1st through Nth). After each elimination round, losers form a consolation bracket (B) and winners form the main bracket (A). Consolation rounds are always scheduled before main bracket rounds at each level. Placement games (final position matches) are ordered worst-to-best position, with the Final always last. Example for 8 teams: QF → B-SF (QF losers) → A-SF (QF winners) → 7th → 5th → 3rd → Final
- Playoffs apply to each group individually; bracket size is determined by the number of teams in the group
- Scheduling uses the tournament-level game length (minutes) for time slot duration and the phase-level start time as the baseline

# Schedule

- The games should be uniformly distributed among courts
- The games within each phase should be uniformly distributed among groups (e.g. the first game should happen at the same time in each group if possible due to amount of courts)
- No team should play or referee in two games at the same time
- No team should referee two consecutive time slots
- In round robin the team should have equal amount of home and away games if possible
- For playoffs, rounds must be scheduled in order (quarterfinals before semifinals before finals)

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

# Results

- It should be possible to enter the game results only using a tournament code
- It should be possible from the phase/group overview and from the court overview
- Game results should be visible in the overviews
- It should be possible to edit a previously recorded result using the same mechanism as entering it
- Results can be entered in two modes:
    - **Detailed** (default): per-set scores (e.g. 25-20, 23-25, 15-10); displayed in overviews when available
    - **Simple**: sets won only (e.g. 2-1); shown as a fallback when per-set data is not recorded
- Tie rules:
    - Simple results (sets-won mode): a tie is not allowed (one team must win more sets)
    - Detailed result with exactly 1 set: a tie is allowed (equal points permitted)
    - Detailed result with more than 1 set: a tie in set count is not allowed (one team must win more sets), and each individual set must also have a winner (no drawn sets)

# Standings

- Standings are calculated from completed game results for each group
- Standings calculation depends on the phase format:

## Round Robin
- 2 points for winning a game, 1 point for a draw, 0 points for losing
- Ordering: win points (descending), then set difference (descending), then direct result between the tied teams
- Set difference = sets won minus sets lost (from HomeScore/AwayScore)
- Direct result tiebreaker: when teams are tied on points and set difference, their head-to-head match result is used
- Teams still tied after all tiebreakers share the same position

## Playoff Elimination
- No point system; positions are determined by the elimination round in which a team lost
- Position formula: `bracketSize / 2^round + 1` where round is 1-indexed
- Multiple teams share the same position (e.g., in an 8-team bracket: 4 QF losers all finish 5th, 2 SF losers finish 3rd)
- The winner of the final gets position 1, the runner-up gets position 2

## Playoff with Placement
- Each team gets a unique final position via placement games
- Placement games are single-game rounds at the end of the bracket (generated worst-to-best, Final last)
- The winner of the Final is 1st, loser is 2nd; winner of 3rd-place game is 3rd, loser is 4th; and so on
- Positions are only assigned for completed placement games

# TBC

# Phase

- Phases structure can be copied from another tournament

