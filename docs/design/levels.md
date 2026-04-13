# Levels — Design Details

Paired with [../requirements/levels.md](../requirements/levels.md).

## Level-scoped split factor

When progressing from a source phase with `sourceLevels` to a target phase with `targetLevels`:

```
splitFactor = targetLevels / sourceLevels
```

Source Level K feeds into target levels:

```
(K - 1) × splitFactor + 1   through   K × splitFactor
```

Example — `sourceLevels = 2`, `targetLevels = 4` → `splitFactor = 2`:
- Source Level 1 → target Levels 1-2
- Source Level 2 → target Levels 3-4

## Cascading level constraint validation

Phase N+1's `NumberOfLevels` must satisfy:

```
numberOfLevels_{N+1} % numberOfLevels_N == 0
```

Valid transitions from Phase N with 2 levels: `2, 4, 6, 8, …`
Invalid: `1, 3, 5, …` (not multiples of 2).

A source phase with no levels (`null` or `0`) imposes no constraint on the next phase.

## Placeholder distribution across target levels

For each source level:

```
expectedPerSourceLevel = placeholderCount / sourceLevels
perTargetLevel = expectedPerSourceLevel / splitFactor
```

Placeholders are seeded 1..N within each target level and assigned via snake draft on auto-assign.
