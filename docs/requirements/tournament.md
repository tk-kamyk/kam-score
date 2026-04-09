# Tournament

- Tournament is represented by:
    - name
    - discipline
    - start time
    - game conditions
    - game length
- It contains a tournament structure, teams, and courts
- The disciplines include Volleyball and Beach Volleyball
- Game conditions are optional and include best-of-sets (allowed values: 1, 3, or 5) and number of winning points in each set
- It should be possible to create, edit, and delete tournaments
- It should be possible to view details of a tournament
- It should be possible to view a list of all tournaments for all users (anonymous and authenticated)
- Tournament codes are only visible to the tournament owner
- Only the tournament owner can edit or delete their tournaments

## Copy Structure from Existing Tournament

- When creating a tournament, the user can optionally select an existing tournament to copy the structure from
- Any tournament can be used as a source (not limited to the user's own tournaments)
- The following is copied from the source tournament:
    - Tournament settings: discipline, game length, game conditions, start time
    - Courts: same count and names as source
    - Structure: all phases with their format, groups, levels, progression config (groupWinners, totalTeamsProceeding), start times
- The following is NOT copied:
    - Real team names and contact info — instead, seed teams are generated (Seed 1, Seed 2, ...) matching the source's real (non-placeholder) team count, with graduated levels
    - Game results and standings
    - Volunteers
    - Tournament code (a new code is generated)
- For phase 2+, placeholder teams are generated from the previous phase's progression config (same as normal phase creation)
- Games are generated and scheduled for all phases using the seed/placeholder teams
    - Phase 1 is set to InProgress status
    - Phase 2+ are set to Scheduled status
    - If a phase is missing prerequisites for game generation (no start time, no game length, no courts, no teams), game generation is skipped for that phase
- The tournament name is provided by the user (not copied from the source)