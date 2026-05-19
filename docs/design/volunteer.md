# Volunteer — Design Details

Paired with [../requirements/volunteer.md](../requirements/volunteer.md). This file captures implementation-level specifics that should **not** appear as user-facing requirements but must be preserved as a spec for engineers.

## Bulk shift-group operations

Both operations are scoped to a single shift group, identified by its name (matching the calculated `ShiftGroup.Name` — phase name, `"Set-up"`, or `"Cleanup"`).

### Clear shift-group assignments

For the named shift group, remove every `ShiftAssignment` whose `ShiftGroup == name` from every volunteer in the tournament. Other shift groups' assignments are untouched. The operation is idempotent — running it on a shift group with no assignments is a no-op.

### Auto-assign — top-up semantics

For the named shift group, bring each shift slot up to the requested per-shift volunteer count `N`:

1. Iterate the shift group's slots in order.
2. For each slot, let `K` be the number of volunteers currently assigned to that exact slot.
3. If `K >= N`, skip the slot — **existing assignments are never displaced**.
4. Otherwise, fetch the ranked candidate list for that slot using the same ranker as the manual flow (`available` desc → `shiftCount` asc → `name` asc — see [Sorting in requirements/volunteer.md](../requirements/volunteer.md#sorting)), filter out volunteers already assigned to this slot, and assign the top `N − K` candidates.
5. **Persist each assignment immediately** before fetching the candidate list for the next slot, so that the just-assigned volunteers' incremented shift count is reflected in the ranking for subsequent slots in the same batch.

### Candidate exhaustion

If, for a given slot, fewer than the needed count of eligible candidates exist (e.g. all remaining volunteers are unavailable or already assigned to that slot), assign as many as possible and continue to the next slot. The operation succeeds without error — partial fill is not a failure mode.

### Special shift groups (Set-up / Cleanup)

Set-up and Cleanup have a single `null`-time slot each. Auto-assign treats them identically to a phase with one slot — it fills that single slot up to `N` using the special-shift ranker (`shiftCount` asc → `name` asc, no availability check, mirroring `GetAvailableVolunteersForSpecialAsync`).

### Non-transactional semantics

Auto-assign performs multiple writes (one per assignment) without a transactional wrapper. If the operation fails partway, intermediate assignments remain persisted. This is acceptable because:
- Shift assignment is not safety-critical state.
- The operation is re-runnable — its top-up semantics make a second run a safe completion of a partial first run.

The frontend retries / surfaces errors via `useSnackbar`; the user can simply re-trigger the action.

### `VolunteersPerShift` bounds

- Minimum: `1` — zero or negative is rejected at validation with `400`.
- Maximum: `50` — defensive upper bound against accidental input (no tournament has shifts that large in practice).

## Endpoint shape

| Method | Route | Body | Service method |
|--------|-------|------|----------------|
| `DELETE` | `/api/tournaments/{tournamentId}/volunteers/shifts/{shiftGroup}/assignments` | — | `VolunteerService.ClearShiftGroupAssignmentsAsync(tournament, shiftGroup)` |
| `POST` | `/api/tournaments/{tournamentId}/volunteers/shifts/{shiftGroup}/auto-assign` | `AutoAssignShiftGroupDto { VolunteersPerShift }` | `VolunteerService.AutoAssignShiftGroupAsync(tournament, shiftGroup, volunteersPerShift)` |

Both endpoints require JWT auth, validated via `TournamentAuthorizationHelper` for owner-or-admin access — matching the existing volunteer mutation endpoints.

If the named shift group does not match any of the calculated shift groups for the tournament, the endpoint returns `404`.
