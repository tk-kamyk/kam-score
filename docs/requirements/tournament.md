# Tournament

- [FR-TRN-001] A tournament is represented by name, discipline, start time, game conditions, and game length.
- [FR-TRN-002] A tournament contains a tournament structure, teams, and courts.
- [FR-TRN-003] Supported disciplines are Volleyball and Beach Volleyball.
- [FR-TRN-004] Game conditions are optional and include best-of-sets (1, 3, or 5) and the number of winning points per set.
- [FR-TRN-005] Tournaments can be created, edited, viewed, and deleted.
- [FR-TRN-006] The list of all tournaments is visible to all users (anonymous and authenticated).
- [FR-TRN-007] The tournament owner is shown on the tournament list and details page.
- [FR-TRN-008] Tournament codes are only visible to the tournament owner.
- [FR-TRN-009] Only the tournament owner (or an admin) can edit or delete a tournament.

## Copy Structure from Existing Tournament

- [FR-TRN-020] When creating a tournament, the user can optionally select any existing tournament (not limited to their own) as a source to copy the structure from.
- [FR-TRN-021] The following is copied from the source: tournament settings (discipline, game length, game conditions, start time), courts (same count and names), and structure (all phases with their format, groups, levels, progression config, and start times).
- [FR-TRN-022] The following is NOT copied: real team names and contact info (replaced by generated seed teams with graduated levels matching the source's real team count), game results and standings, volunteers, and the tournament code (a fresh code is generated).
- [FR-TRN-023] The tournament name is provided by the user, not copied.
- [FR-TRN-024] For phase 2+, placeholder teams are generated from the previous phase's progression config (same as normal phase creation).
- [FR-TRN-025] Games are generated and scheduled for all phases using seed/placeholder teams: phase 1 is set to `InProgress`, phase 2+ to `Scheduled`. If a phase is missing prerequisites for generation (no start time, no game length, no courts, or no teams), generation is skipped for that phase.
