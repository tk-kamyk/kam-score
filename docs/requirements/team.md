# Team

- [FR-TEAM-001] A team is defined by name, level (0-100), and contact information (email, phone).
- [FR-TEAM-002] Contact information is only visible to admins, not to standard users.
- [FR-TEAM-003] Teams can be created, edited, and deleted.
- [FR-TEAM-004] The list of teams participating in a tournament is visible to all users.

## Generate Seed Teams

- [FR-TEAM-010] The owner can generate N seed teams for a tournament in a single action; only the owner (or an admin) can do this.
- [FR-TEAM-011] Generated teams are real (non-placeholder) teams named "Seed 1", "Seed 2", …; numbering is additive, starting from `existing real team count + 1`.
- [FR-TEAM-012] Levels are distributed proportionally across 0–100 (1 team → 50; N teams → evenly spaced 0..100, e.g. 4 teams → 0, 33, 67, 100).
- [FR-TEAM-013] Count must be between 1 and 100.
- [FR-TEAM-014] The organiser can later edit each generated team to replace its name, adjust its level, and add contact info.

## Team Schedule & Participation

- [FR-TEAM-020] Each team row in the list is expandable; expanding shows the team's schedule overview grouped by phase (phase name, format, level/group as section headers).
- [FR-TEAM-021] Each game in the schedule shows time, court, opponent, result, and the team's role (Home / Away / Referee).
- [FR-TEAM-022] A "Show breaks" toggle (on by default) reveals time slots where the team has no role.
- [FR-TEAM-023] The API supports filtering games by team (returns games where the team plays or referees); game responses include phase name, group name, and level name for display.
