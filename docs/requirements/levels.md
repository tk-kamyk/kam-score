# Levels

> See design: [../design/levels.md](../design/levels.md) for the level-scoped progression split-factor algorithm.

## Basic properties

- A phase can optionally define levels via `NumberOfLevels` (null or 0 means no levels)
- When levels are defined, groups are evenly distributed across levels
- `NumberOfGroups` represents groups **per level** (total groups = `NumberOfGroups × NumberOfLevels`)
- Each level has a default name ("Level 1", "Level 2") that can be customized
- Level names must be unique within a phase
- Levels have an order (1-based) determining their ranking priority
- When levels are not defined, everything behaves as without levels (backward compatible)
- Levels are structural — the number of levels cannot be changed while games exist
- Level names can be updated independently of other phase properties
- Levels cannot be modified on a completed phase

## Cascading level constraint

- Levels cascade forward through phases: once a phase defines levels, subsequent phases must maintain at least as many levels
- Phase N+1's `NumberOfLevels` must be a **multiple** of Phase N's level count
- If Phase N has no levels, Phase N+1 can freely introduce levels or have none
- Validation is applied when adding a new phase — invalid level counts return HTTP 400
- When deleting a phase, re-validate that the gap doesn't break the constraint between the now-adjacent phases

## Auto-assign with levels

- Auto-assign respects levels: teams are split by seed into levels (top half → Level 1, bottom half → Level 2), then snake-drafted within each level's groups
- Manual team assignment has no level restrictions — teams can be freely assigned to any group regardless of level

## Progression with levels

- At phase completion, rankings incorporate levels (all Level 1 teams ranked above Level 2)
- When levels are defined, `GroupWinners` and `TotalTeamsProceeding` apply **per level**. Total teams advancing = progression config × number of levels

## Level-scoped progression (cross-phase)

- When levels increase between phases, progression uses a **level-scoped split**: qualifying teams from a source level feed into a contiguous range of target levels
- When the source phase has no levels but the target has levels, all qualifying teams are treated as a single pool and distributed across target levels by seed
- When both phases have the same number of levels, progression works level-to-level (Level 1 → Level 1, etc.)
- Within each target level, teams are seeded by their qualifying rank and distributed via snake draft

## Placeholder generation with level split

- Placeholder generation respects the level-scoped split: when levels increase, placeholders are distributed across target levels proportionally
- Each source level's expected team count is split evenly across its corresponding target levels
- Placeholder naming includes the source level context (see [phase-advancement.md](./phase-advancement.md))
