# Phase Advancement — design

Paired with [../requirements/phase-advancement.md](../requirements/phase-advancement.md).

## Placeholder count

```
placeholderCount = TotalTeamsProceeding ?? (GroupWinners × number of groups in source phase)
```

## Placeholder naming

```
"{SourcePhaseName} - Seed {N}"
```

With levels ([see levels.md](../requirements/levels.md)):

```
"{SourcePhaseName} - {SourceLevelName} - Seed {N}"
```

## [FR-ADV-040] Placeholder resolution algorithm

When a phase is marked Completed:

1. Compute qualifying teams per the progression rules. Group winners (the top `GroupWinners` finishers from each group) qualify automatically; remaining slots are filled by ranking non-winners across groups.
2. Rank the qualifying teams into a single seeded list using one of two cross-group modes:
   - **Group-position-major** (when both `GroupWinners` and `TotalTeamsProceeding` are set): group position is the primary key, with the format's standings cascade as a tiebreaker within each position tier. Every group winner ranks ahead of every runner-up.
   - **Stats-only** (when only `TotalTeamsProceeding` is set): the standings cascade ranks teams flat; group position is not considered.
3. For each placeholder in the next phase, ordered by `Seed`: set its `ResolvedTeamId` to the corresponding real team ID, and replace placeholder ID occurrences in next-phase games (`HomeTeamId`, `AwayTeamId`, `RefereeTeamId`) and groups (`TeamIds`).

Reopening a Completed phase reverses this by walking resolved placeholders and restoring the placeholder IDs into games and groups, clearing `ResolvedTeamId`.

## [FR-ADV-045] Interaction with within-phase playoff placeholders

`HomeTeamPlaceholder` / `AwayTeamPlaceholder` strings (e.g. `"Winner SF1"`, `"Loser QF2"`) describe bracket advancement **within** a phase. They are resolved when a playoff game result is recorded and are orthogonal to the cross-phase placeholder teams above.
