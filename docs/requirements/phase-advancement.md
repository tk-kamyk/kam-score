# Phase Advancement

> See design: [../design/phase-advancement.md](../design/phase-advancement.md) for placeholder-resolution algorithm and seeding order specifics.

## Phase status

- Each phase has a status: `New` (default), `InProgress`, `Completed`
- **New → InProgress**: for the first phase, this happens when games are generated; for subsequent phases, when the previous phase completes (placeholder IDs are resolved to real team IDs)
    - For a `Custom` phase, the owner triggers the transition directly from the same action used for generation (no games are created); all groups must have at least one team assigned
- **InProgress → Completed**: owner explicitly marks the phase as complete via the Schedule tab
- Completing a phase requires all games in the phase to have results recorded
    - For a `Custom` phase, completion instead requires that every group has a complete manual standings order (every assigned team included exactly once)
- Reopening a completed phase reverts it to `InProgress` and reverses placeholder resolution in the next phase (real IDs swapped back to placeholder IDs, next phase reverts to `New`)
    - Reopening a `Custom` phase also unlocks the manual standings for re-editing; re-completing triggers placeholder resolution again with the updated order

## Progression

- Phase defines the amount of teams progressing via `GroupWinners` and/or `TotalTeamsProceeding`
- The progressing teams are the input to the next phase
- Progression is automatic when the phase is marked as completed:
    1. From each group, the top `GroupWinners` teams qualify automatically
    2. Remaining teams across all groups are ranked together
    3. Best remaining teams fill slots until `TotalTeamsProceeding` is reached
    4. If only `GroupWinners` is set: total qualifying = `GroupWinners × number of groups`
    5. If only `TotalTeamsProceeding` is set: top N teams across all groups qualify
    6. If neither is set: no progression occurs
    7. Setting either to 0 explicitly marks the phase as final — no teams advance, but the phase is recognized as having progression config for final standings calculation
- Cross-group ranking rules:
    - When both `GroupWinners` and `TotalTeamsProceeding` are set, the cross-group ranking is **group-position-major**: all teams that finished at the same position rank ahead of teams at a worse position, with the standings-criteria cascade applied as a tiebreaker within each position tier.
    - When only `TotalTeamsProceeding` is set, the cross-group ranking uses the standings-criteria cascade only (group position is not considered).
- All qualifying teams are ranked together in a single seeding order — this produces Seed 1, Seed 2, ..., Seed N
- Seeded teams are assigned to the next phase's groups via snake draft
- When the completing phase uses the `Custom` format, the qualifying set and seeding order are derived from the manually entered positions per group; no other standings criteria apply

## Placeholder teams

- When a phase with order > 1 is created and the previous phase has progression config, placeholder Team entities are automatically generated
- Each placeholder is a real Team entity with `IsPlaceholder = true`, a `SourcePhaseId`, and a `Seed` (1-based)
- Placeholders are fully functional: they can be assigned to groups, and games can be generated and scheduled using their IDs
- Auto-assign for phase 2+: placeholder teams ordered by seed, distributed via snake draft
- When the previous phase's progression config changes, old placeholders are deleted and regenerated; any existing games in the next phase are also deleted
- When a phase is deleted, its associated placeholder teams are also deleted

## Placeholder resolution

- When a phase is marked as completed, placeholder team IDs in the **next** phase are swapped to real team IDs based on the seeding order (Seed 1 → best qualifying team, etc.)
- Swaps occur in: game `HomeTeamId`, `AwayTeamId`, `RefereeTeamId`, and group `TeamIds`
- Each placeholder team's `ResolvedTeamId` is set to the real team it resolved to
- Reopening a completed phase reverses the swap and clears `ResolvedTeamId`; the next phase reverts to `New`
- When placeholders are resolved, structure editing for the next phase shows resolved real team names
- Within-phase playoff placeholders (`HomeTeamPlaceholder` / `AwayTeamPlaceholder` strings like "Winner SF1") are separate from cross-phase placeholder teams and are not touched by resolution
