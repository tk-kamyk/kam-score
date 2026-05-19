# Volunteer

> See design: [./design/volunteer.md](../design/volunteer.md)

- Volunteer is defined by:
    - name (required, free text, unique per tournament)
    - contact (optional, free text)
    - team (optional, from the list of teams in the tournament)
- It should be possible to create, edit, and delete volunteers
- It should be possible to see a list of volunteers for a tournament
- Volunteer names must be unique within a tournament (case-insensitive)
- Only the tournament owner or system admin can create, edit, delete, and view volunteers
- Volunteers are NOT visible to anonymous or participant users
- Volunteers are stored in a dedicated Cosmos DB container (`volunteers`), partitioned by `tournamentId`
- Deleting a tournament deletes all associated volunteers
- When a team is deleted, volunteers linked to that team have their team reference cleared (set to null)

## Shift Assignment

- The shift view is divided into **shift groups**, one per tournament phase plus Set-up and Cleanup:
  - **Set-up**: a single shift before the first phase (no availability/play indicators)
  - **Phase 1, Phase 2, ... Phase N**: one group per tournament phase, with multiple shifts
  - **Cleanup**: a single shift after the last phase (no availability/play indicators)
- Shift groups are always shown, regardless of configuration
- If `Tournament.GameLength` is missing or a phase has no `StartTime`, that phase is displayed as a single shift (same as Set-up/Cleanup — no time slots, no availability indicators)

### Phase shift calculation

- For each phase that has a `StartTime` and the tournament has a `GameLength`:
  - Shifts start at the phase's `StartTime` and step by `Tournament.GameLength` minutes
  - The last shift of a phase is bounded by the next phase's `StartTime` — shifts that don't fit are not created
    - Example: if game length is 20 min and a phase runs from 10:00 to 11:30, there are 4 shifts (10:00, 10:20, 10:40, 11:00) — the remaining 10 min gap is ignored
  - For the last phase (no next phase), the number of shifts equals the number of game rounds in that phase
- Set-up and Cleanup never have time slots — they are always single shifts with no time displayed
- Phases without `StartTime` or without `GameLength` are displayed as a single shift with no time slots

### Assignment

- Multiple volunteers can be assigned to the same shift
- A volunteer can be assigned to multiple shifts
- Assignments are stored as a list of shift identifiers on the Volunteer entity (shift group + time)
- Only valid shift times (matching the computed slots) can be assigned

### Availability (phase shifts only, not Set-up/Cleanup)

- A volunteer is **unavailable** for a shift if their linked team is playing (home or away) or refereeing in a game whose `StartTime` matches the shift time
- Volunteers with no linked team are always available
- **Plays before**: the volunteer's team has a game in the previous time slot (shift time minus game length)
- **Plays after**: the volunteer's team has a game in the next time slot (shift time plus game length)
- If a volunteer is assigned to a shift and their team later gets a game at that time, the assignment is kept with a visual warning (not auto-removed)

### Sorting

- Available volunteers are sorted by: available first, then fewest total shifts, then alphabetically by name

### Bulk shift-group operations

- The tournament owner or system admin can clear all volunteer assignments for a given shift group (a phase, Set-up, or Cleanup) in a single action
- The tournament owner or system admin can auto-assign volunteers to all shifts in a given shift group, specifying how many volunteers each shift should have

### UI

- The Volunteers tab has two sub-tabs: "List" (existing CRUD) and "Shifts" (shift assignment view)
- The Shifts view is organized into collapsible sections per shift group (Set-up, Phase 1, ..., Cleanup)
- Each section shows shift time slots with assigned volunteer chips
- Assigned volunteers whose team is playing/refereeing at that time are shown with a warning indicator
- An assignment dialog shows all volunteers with availability info, shift count, and plays-before/after indicators
- Set-up and Cleanup assignment dialogs show all volunteers without availability/play indicators
