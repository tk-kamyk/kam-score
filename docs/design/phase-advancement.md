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

1. Compute qualifying teams per the progression rules (group winners + best remaining via tiebreaker cascade)
2. Rank all qualifying teams into a single seeded list (Seed 1 = best overall)
3. For each placeholder in the next phase (ordered by `Seed`):
   - Set its `ResolvedTeamId` to the corresponding real team ID
   - In all next-phase games: replace `HomeTeamId`, `AwayTeamId`, `RefereeTeamId` occurrences of the placeholder's ID with the resolved team ID
   - In all next-phase groups: replace occurrences of the placeholder's ID in `TeamIds`

Reopening a Completed phase reverses this by walking resolved placeholders and restoring the placeholder IDs into games/groups, clearing `ResolvedTeamId`.

## Interaction with within-phase playoff placeholders

`HomeTeamPlaceholder` / `AwayTeamPlaceholder` strings (e.g. `"Winner SF1"`, `"Loser QF2"`) live on individual game entities and describe bracket advancement **within** a phase. They are resolved by the bracket-advancement service when a playoff game result is recorded and are **orthogonal** to the cross-phase placeholder teams described above.
