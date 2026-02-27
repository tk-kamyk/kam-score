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
    - games (TBC)
    - standings (TBC)
- Group name is unique within one phase
- Teams can be assigned to and removed from groups individually
- A team cannot be assigned to two groups in the same phase

# Game

- Game is defined by the teams participating, the refereeing team, the court, the start time, and the result
- It should be possible to enter the game results only using a tournament code. A tournament code should consist of 4 digits/letters and be visible to authenticated users on the tournament page. Then the code is distributed to participants outside of the application.


# TBC

# Phase

- It is possible to get a list of all games within all the groups of a phase
- Phases structure can be copied from another tournament

# Group

- Groups collect teams, games, and standings

# Schedule

- It should be possible to automatically schedule all the games in a phase
- The games should be uniformly distributed among courts
- The games within each phase should uniformly distributed among groups (e.g. the first game should happen at the same time in each group if possible due to amount of courts)
- Team should not play two games in a row
- Team should not play just after refereeing
- In round robin the team should have equal amount of home and away games if possible
- It should be possible to get a schedule overview with all the courts
- It should be possible to introduce 'break' periods in the schedule where no games can be scheduled