# Volunteer — design

Paired with [../requirements/volunteer.md](../requirements/volunteer.md).

## [FR-VOL-011] Phase shift calculation

For each phase that has a `StartTime` and a tournament `GameLength`:

- Shifts start at the phase's `StartTime` and step by `GameLength` minutes.
- The last shift of a phase is bounded by the next phase's `StartTime` — shifts that don't fit are not created. *Example:* game length 20 min, phase 10:00–11:30 → 4 shifts at 10:00, 10:20, 10:40, 11:00; the trailing 10-minute gap is ignored.
- For the last phase (no next phase), the number of shifts equals the number of game rounds in that phase.

Set-up and Cleanup never have time slots — they are single shifts with no time displayed. Phases without `StartTime` or without `GameLength` collapse to a single shift with no time slots.

## [FR-VOL-050] Clear shift-group assignments

For the named shift group, remove every assignment whose shift group matches from every volunteer in the tournament. Other shift groups are untouched. The operation is idempotent — running it on an empty shift group is a no-op.

## [FR-VOL-051] Auto-assign — top-up semantics

For the named shift group, bring each slot up to the requested per-shift volunteer count `N`:

1. Iterate the slots in order.
2. For each slot, let `K` be the number of volunteers already assigned to it. If `K ≥ N`, skip the slot — **existing assignments are never displaced**.
3. Otherwise, fetch the ranked candidate list using the same ranker as the manual flow (available desc → shift count asc → name asc — see [Sorting in requirements/volunteer.md](../requirements/volunteer.md#sorting)), exclude volunteers already on the slot, and assign the top `N − K`.
4. **Persist each assignment immediately** before fetching candidates for the next slot, so a just-assigned volunteer's incremented shift count is reflected in the ranking for subsequent slots in the same batch.

**Candidate exhaustion** — when fewer than the needed count of eligible candidates exist for a slot, assign as many as possible and continue to the next slot. Partial fill is not a failure mode.

**Set-up / Cleanup** — single-slot shift groups; auto-assign fills that one slot up to `N` using the special-shift ranker (shift count asc → name asc, no availability check).

## Non-transactional semantics

Auto-assign performs multiple writes (one per assignment) without a transactional wrapper. If the operation fails partway, intermediate assignments remain persisted. This is acceptable because (a) shift assignment is not safety-critical state and (b) the operation is re-runnable — its top-up semantics make a second run a safe completion of a partial first run. The frontend surfaces errors and the user can simply re-trigger the action.

## `VolunteersPerShift` bounds

- Minimum: `1` — zero or negative is rejected at validation with 400.
- Maximum: `50` — defensive upper bound against accidental input.

## Endpoint shape

| Method | Route | Body |
|--------|-------|------|
| `DELETE` | `/api/tournaments/{tournamentId}/volunteers/shifts/{shiftGroup}/assignments` | — |
| `POST` | `/api/tournaments/{tournamentId}/volunteers/shifts/{shiftGroup}/auto-assign` | `{ volunteersPerShift }` |

Both require JWT auth with owner-or-admin access. If the named shift group does not match any of the calculated shift groups for the tournament, the endpoint returns 404.
