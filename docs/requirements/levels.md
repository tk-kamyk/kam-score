# Levels

> See design: [../design/levels.md](../design/levels.md) for the level-scoped progression split-factor algorithm.

## Basic properties

- [FR-LVL-001] A phase can optionally define levels via `NumberOfLevels` (null or 0 means no levels).
- [FR-LVL-002] When levels are defined, groups are evenly distributed across levels; `NumberOfGroups` represents groups per level (total groups = `NumberOfGroups × NumberOfLevels`).
- [FR-LVL-003] Each level has a default name ("Level 1", "Level 2") that can be customised; level names are unique within a phase.
- [FR-LVL-004] Levels have an order (1-based) determining ranking priority.
- [FR-LVL-005] When levels are not defined, everything behaves as without levels (backward compatible).
- [FR-LVL-006] Levels are structural — the number of levels cannot be changed while games exist; level names can be updated independently.
- [FR-LVL-007] Levels cannot be modified on a completed phase.

## Cascading level constraint

- [FR-LVL-010] Levels cascade forward: once a phase defines levels, subsequent phases must maintain at least as many levels.
- [FR-LVL-011] Phase N+1's `NumberOfLevels` must be a multiple of Phase N's level count.
- [FR-LVL-012] If Phase N has no levels, Phase N+1 can freely introduce levels or have none.
- [FR-LVL-013] Validation is applied when adding a phase — invalid level counts return HTTP 400.
- [FR-LVL-014] When a phase is deleted, the constraint is re-validated against the now-adjacent phases.

## Auto-assign with levels

- [FR-LVL-020] Auto-assign respects levels: teams are split by seed into levels (top half → Level 1, bottom half → Level 2), then snake-drafted within each level's groups.
- [FR-LVL-021] Manual team assignment has no level restrictions — teams can be assigned to any group regardless of level.

## Progression with levels

- [FR-LVL-030] At phase completion, rankings incorporate levels (all Level 1 teams ranked above Level 2).
- [FR-LVL-031] When levels are defined, `GroupWinners` and `TotalTeamsProceeding` apply per level; total teams advancing = progression config × number of levels.

## Level-scoped progression (cross-phase)

- [FR-LVL-040] When levels increase between phases, progression uses a level-scoped split: qualifying teams from a source level feed into a contiguous range of target levels.
- [FR-LVL-041] When the source phase has no levels but the target has levels, all qualifying teams are treated as a single pool and distributed across target levels by seed.
- [FR-LVL-042] When both phases have the same number of levels, progression works level-to-level (Level 1 → Level 1, etc.).
- [FR-LVL-043] Within each target level, teams are seeded by qualifying rank and distributed via snake draft.

## Placeholder generation with level split

- [FR-LVL-050] Placeholder generation respects the level-scoped split: when levels increase, placeholders are distributed across target levels proportionally.
- [FR-LVL-051] Each source level's expected team count is split evenly across its corresponding target levels.
- [FR-LVL-052] Placeholder naming includes the source level context (see [phase-advancement.md](./phase-advancement.md)).
