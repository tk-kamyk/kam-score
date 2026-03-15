# Team

- Team is defined by:
    - name
    - level (0-100)
    - contact information (email, phone number)
- Contact information should only be visible to admins, not standard users
- It should be possible to create, edit, and delete teams
- It should be possible to see a list of teams participating in the tournament

## Team Schedule & Participation

- Each team row in the list should be expandable (click to expand/collapse)
- When expanded, the team's schedule overview is displayed:
  - Games are grouped by phase (with phase name, format, and level/group as section headers)
  - Each game shows: time, court, opponent, result, and the team's role (Home / Away / Referee)
  - A "Show breaks" toggle (off by default) reveals time slots where the team has no role
- The phase/group participation info is shown as headers within the expanded schedule area
- The API supports filtering games by team (returns games where the team plays or referees)
- Game responses include phase name, group name, and level name for display purposes