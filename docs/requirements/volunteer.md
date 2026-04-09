# Volunteer

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
