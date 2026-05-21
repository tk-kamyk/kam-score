# Phase Advancement

> See design: [../design/phase-advancement.md](../design/phase-advancement.md) for placeholder-resolution algorithm and seeding order specifics.

## Phase status

- [FR-ADV-001] Each phase has a status: `New` (default), `InProgress`, `Completed`.
- [FR-ADV-002] **New → InProgress**: for the first phase, when games are generated; for subsequent phases, when the previous phase completes (placeholder IDs are resolved to real team IDs).
- [FR-ADV-003] For a `Custom` phase, the owner triggers `New → InProgress` directly from the same action used for generation (no games are created); all groups must have at least one team assigned.
- [FR-ADV-004] **InProgress → Completed**: the owner explicitly marks the phase complete from the Schedule tab.
- [FR-ADV-005] Completing a phase requires all games in the phase to have results recorded.
- [FR-ADV-006] Completing a `Custom` phase requires every group to have a complete manual standings order (every assigned team included exactly once).
- [FR-ADV-007] Reopening a completed phase reverts it to `InProgress` and reverses placeholder resolution in the next phase (real IDs swapped back to placeholder IDs; next phase reverts to `New`).
- [FR-ADV-008] Reopening a `Custom` phase also unlocks the manual standings for re-editing; re-completing triggers placeholder resolution again with the updated order.

## Progression

- [FR-ADV-010] A phase defines the number of teams progressing via `GroupWinners` and/or `TotalTeamsProceeding`; the progressing teams feed the next phase.
- [FR-ADV-011] Progression is automatic when the phase is marked completed: from each group, the top `GroupWinners` teams qualify automatically; remaining teams across all groups are ranked together; best remaining fill slots until `TotalTeamsProceeding` is reached.
- [FR-ADV-012] If only `GroupWinners` is set: total qualifying = `GroupWinners × number of groups`.
- [FR-ADV-013] If only `TotalTeamsProceeding` is set: top N teams across all groups qualify.
- [FR-ADV-014] If neither is set: no progression occurs.
- [FR-ADV-015] Setting either to 0 explicitly marks the phase as final — no teams advance, but the phase is recognised as having progression config for final standings calculation.
- [FR-ADV-016] When both `GroupWinners` and `TotalTeamsProceeding` are set, cross-group ranking is group-position-major: teams at the same position rank ahead of teams at a worse position, with the standings cascade applied as a tiebreaker within each position tier.
- [FR-ADV-017] When only `TotalTeamsProceeding` is set, cross-group ranking uses the standings cascade alone (group position is not considered).
- [FR-ADV-018] All qualifying teams are ranked into a single seeding order (Seed 1 … Seed N) and assigned to the next phase's groups via snake draft.
- [FR-ADV-019] When the completing phase uses the `Custom` format, the qualifying set and seeding order are derived from the manually entered positions per group; no other standings criteria apply.

## Placeholder teams

- [FR-ADV-030] When a phase with order > 1 is created and the previous phase has progression config, placeholder Team entities are automatically generated.
- [FR-ADV-031] Each placeholder is a real Team entity with `IsPlaceholder = true`, a `SourcePhaseId`, and a `Seed` (1-based).
- [FR-ADV-032] Placeholders are fully functional: they can be assigned to groups, and games can be generated and scheduled using their IDs.
- [FR-ADV-033] Auto-assign for phase 2+: placeholders ordered by seed, distributed via snake draft.
- [FR-ADV-034] When the previous phase's progression config changes, old placeholders are deleted and regenerated; any existing games in the next phase are also deleted.
- [FR-ADV-035] When a phase is deleted, its associated placeholder teams are also deleted.

## Placeholder resolution

- [FR-ADV-040] When a phase is marked completed, placeholder IDs in the next phase are swapped to real team IDs in seeding order (Seed 1 → best qualifying team, etc.).
- [FR-ADV-041] Swaps occur in game `HomeTeamId`, `AwayTeamId`, `RefereeTeamId`, and group `TeamIds`.
- [FR-ADV-042] Each placeholder team's `ResolvedTeamId` is set to the real team it resolved to.
- [FR-ADV-043] Reopening a completed phase reverses the swap and clears `ResolvedTeamId`; the next phase reverts to `New`.
- [FR-ADV-044] When placeholders are resolved, structure editing for the next phase shows resolved real team names.
- [FR-ADV-045] Within-phase playoff placeholders (`HomeTeamPlaceholder` / `AwayTeamPlaceholder` strings like "Winner SF1") are separate from cross-phase placeholder teams and are not touched by resolution.
