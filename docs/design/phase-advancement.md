# Phase Advancement — Design Details

Paired with [../requirements/phase-advancement.md](../requirements/phase-advancement.md).

## Placeholder count calculation

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

## Placeholder resolution algorithm

When a phase is marked Completed:

1. Compute qualifying teams per the progression rules. Group winners (the top `GroupWinners` finishers from each group) qualify automatically; remaining slots are filled by ranking non-winners across groups.
2. Rank the qualifying teams into a single seeded list. Two cross-group ranking modes exist:
   - **Group-position-major** (when both `GroupWinners` and `TotalTeamsProceeding` are set): group position is the primary key (all position-1 teams before all position-2 teams, etc.), with the format's standings-criteria cascade as a tiebreaker within each position tier. This places every group winner ahead of every runner-up.
   - **Stats-only** (when only `TotalTeamsProceeding` is set): the format's standings-criteria cascade ranks teams flat; group position is not considered.
3. For each placeholder in the next phase (ordered by `Seed`):
   - Set its `ResolvedTeamId` to the corresponding real team ID
   - In all next-phase games: replace `HomeTeamId`, `AwayTeamId`, `RefereeTeamId` occurrences of the placeholder's ID with the resolved team ID
   - In all next-phase groups: replace occurrences of the placeholder's ID in `TeamIds`

Reopening a Completed phase reverses this by walking resolved placeholders and restoring the placeholder IDs into games/groups, clearing `ResolvedTeamId`.

## Interaction with within-phase playoff placeholders

`HomeTeamPlaceholder` / `AwayTeamPlaceholder` strings (e.g. `"Winner SF1"`, `"Loser QF2"`) live on individual game entities and describe bracket advancement **within** a phase. They are resolved by the bracket-advancement service when a playoff game result is recorded and are **orthogonal** to the cross-phase placeholder teams described above.
