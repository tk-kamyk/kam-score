# Volunteer

> See design: [./design/volunteer.md](../design/volunteer.md) for shift-calculation procedure, bulk operations, and endpoint shapes.

- [FR-VOL-001] A volunteer is defined by name (required, free text, unique per tournament, case-insensitive), contact (optional, free text), and team (optional, drawn from the tournament's teams).
- [FR-VOL-002] Volunteers can be created, edited, deleted, and listed.
- [FR-VOL-003] Only the tournament owner or system admin can create, edit, delete, or view volunteers; volunteers are not visible to anonymous or participant users.
- [FR-VOL-004] Volunteers are persisted per tournament; deleting a tournament deletes all associated volunteers.
- [FR-VOL-005] When a team is deleted, volunteers linked to that team have their team reference cleared.

## Shift assignment

- [FR-VOL-010] The shift view is divided into shift groups: Set-up (single shift before the first phase), one group per tournament phase, and Cleanup (single shift after the last phase). Shift groups are always shown, regardless of configuration.
- [FR-VOL-011] Shifts within a phase group are derived from the phase's start time and the tournament's game length; Set-up and Cleanup are single-shift groups with no time slots and no availability indicators.
- [FR-VOL-012] If `Tournament.GameLength` is missing or a phase has no `StartTime`, that phase is displayed as a single shift (same as Set-up/Cleanup — no time slots, no availability indicators).

### Assignment

- [FR-VOL-020] Multiple volunteers can be assigned to the same shift; a volunteer can be assigned to multiple shifts.
- [FR-VOL-021] Assignments are stored as a list of shift identifiers (shift group + time) on the volunteer.
- [FR-VOL-022] Only valid shift times (matching the computed slots) can be assigned.

### Availability (phase shifts only, not Set-up/Cleanup)

- [FR-VOL-030] A volunteer is **unavailable** for a shift if their linked team is playing (home or away) or refereeing in a game whose `StartTime` matches the shift time.
- [FR-VOL-031] Volunteers with no linked team are always available.
- [FR-VOL-032] **Plays before**: the volunteer's team has a game in the previous time slot (shift time minus game length).
- [FR-VOL-033] **Plays after**: the volunteer's team has a game in the next time slot (shift time plus game length).
- [FR-VOL-034] If a volunteer is assigned to a shift and their team later gets a game at that time, the assignment is kept with a visual warning (not auto-removed).

### Sorting

- [FR-VOL-040] Available volunteers are sorted by: available first, then fewest total shifts, then alphabetically by name.

### Bulk shift-group operations

- [FR-VOL-050] The tournament owner or system admin can clear all volunteer assignments for a given shift group (phase, Set-up, or Cleanup) in a single action.
- [FR-VOL-051] The tournament owner or system admin can auto-assign volunteers to all shifts in a given shift group, specifying how many volunteers each shift should have.

### UI

- [FR-VOL-060] The Volunteers tab has two sub-tabs: "List" (CRUD) and "Shifts" (shift assignment view).
- [FR-VOL-061] The Shifts view is organised into collapsible sections per shift group; each section shows shift time slots with assigned volunteer chips.
- [FR-VOL-062] Assigned volunteers whose team is playing/refereeing at that time are shown with a warning indicator.
- [FR-VOL-063] An assignment dialog lists all volunteers with availability info, shift count, and plays-before/after indicators.
- [FR-VOL-064] Set-up and Cleanup assignment dialogs show all volunteers without availability/play indicators.
