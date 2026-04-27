# Phase State Restrictions

Operations on phases, groups, teams, and courts are restricted based on phase status and game existence to protect data integrity. All state violations return **HTTP 409 Conflict**. Input/validation errors return **HTTP 400**.

## Completed phases (status = Completed)

Cannot:
- Edit (update name, format, start time, progression config)
- Delete the phase
- Add, rename, or delete groups
- Assign or remove teams, or auto-assign teams
- Delete games
- Record game results

To make any changes, the phase must be **reopened** first.

## Phases with generated games (InProgress, or New with games)

Cannot:
- Change structural fields (format, start time)
- Add or delete groups
- Assign, remove, or auto-assign teams
- Delete the phase

Can:
- Change non-structural fields (name, `groupWinners`, `totalTeamsProceeding`)
- Rename groups

To modify structure, **delete games first**.

## Game deletion resets phase status

- When games are deleted from an `InProgress` phase, the phase status resets to `New`

## Reopen guard

- Cannot reopen a phase if the next phase has any completed games
- Delete results in the next phase first

## Result recording guard

- Cannot record a result when either team is unassigned (null team ID, e.g. unresolved playoff placeholders) — returns HTTP 400

## Custom phase prerequisites

- A `Custom` phase can be started without a tournament game length, a phase start time, or any courts configured (no games are scheduled)
- A `Custom` phase can be started only when every group has at least one team assigned — same rule as other formats
- Manual standings for a group can only be edited while the phase is `InProgress`; the order must list every team currently in the group exactly once
- Completing a `Custom` phase requires every group to have a complete manual standings order

## Referential integrity

- Cannot delete a team that is assigned to any group or referenced in any game
- Cannot delete a court that has scheduled games
- Remove references first (unassign team, delete games)
