# Phase State Restrictions

Operations on phases, groups, teams, and courts are restricted based on phase status and game existence to protect data integrity. All state violations return **HTTP 409 Conflict**. Input/validation errors return **HTTP 400**.

## Completed phases (`Status = Completed`)

- [FR-PSR-001] Cannot edit a completed phase (name, format, start time, progression config).
- [FR-PSR-002] Cannot delete a completed phase.
- [FR-PSR-003] Cannot add, rename, or delete groups in a completed phase.
- [FR-PSR-004] Cannot assign or remove teams, or run auto-assign, in a completed phase.
- [FR-PSR-005] Cannot delete games or record game results in a completed phase.
- [FR-PSR-006] To make any changes, the phase must be reopened first.

## Phases with generated games (`InProgress`, or `New` with games)

- [FR-PSR-010] Cannot change structural fields (format, start time) once games exist.
- [FR-PSR-011] Cannot add or delete groups once games exist.
- [FR-PSR-012] Cannot assign, remove, or auto-assign teams once games exist.
- [FR-PSR-013] Cannot delete the phase once games exist.
- [FR-PSR-014] Can change non-structural fields (name, `GroupWinners`, `TotalTeamsProceeding`) and rename groups.
- [FR-PSR-015] To modify structure, delete games first.

## Status transitions

- [FR-PSR-020] When games are deleted from an `InProgress` phase, the phase status resets to `New`.
- [FR-PSR-021] Cannot reopen a phase if the next phase has any completed games; delete those results first.
- [FR-PSR-022] Cannot record a result when either team is unassigned (e.g. unresolved playoff placeholders) — returns HTTP 400.

## Custom phase prerequisites

- [FR-PSR-030] A `Custom` phase can be started without a tournament game length, a phase start time, or any courts (no games are scheduled).
- [FR-PSR-031] A `Custom` phase can be started only when every group has at least one team assigned (same rule as other formats).
- [FR-PSR-032] Manual standings for a group can only be edited while the phase is `InProgress`; the order must list every team currently in the group exactly once.
- [FR-PSR-033] Completing a `Custom` phase requires every group to have a complete manual standings order.

## Referential integrity

- [FR-PSR-040] Cannot delete a team that is assigned to any group or referenced in any game.
- [FR-PSR-041] Cannot delete a court that has scheduled games.
- [FR-PSR-042] Remove references first (unassign team, delete games).
