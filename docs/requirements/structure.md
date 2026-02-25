# Phase

- Tournament phase defines a part of tournament
- It contains groups - minimum one
- It defines what is format of games to be played - round robin, play-off elimination, play-off with placement games
- In the first phase, the teams are assigned to groups based on their seed or manually (e.g. standard seeding, top together, etc.)
- In each phase, each resulting position is assigned a unique identifier, e.g. phaseA-groupA-position1
- For the next phases, it is possible to define where each position proceeds to - which phase and which group
- It is possible to get a list of all games within all the groups of a phase
- Phases structure can be copied from another tournament

# Group

- Groups collect teams, games, and standings

# Game

- Game is defined by the teams participating, the refereeing team, the court, the start time, and the result
- It should be possible to enter the game results only using a tournament code. A tournament code should consist of 4 digits/letters and be visible to authenticated users on the tournament page. Then the code is distributed to participants outside of the application.

# Schedule

- It should be possible to automatically schedule all the games in a phase
- The games should be uniformly distributed among courts
- The games within each phase should uniformly distributed among groups (e.g. the first game should happen at the same time in each group if possible due to amount of courts)
- Team should not play two games in a row
- Team should not play just after refereeing
- In round robin the team should have equal amount of home and away games if possible
- It should be possible to get a schedule overview with all the courts
- It should be possible to introduce 'break' periods in the schedule where no games can be scheduled