# User

## Permission tiers

### Anonymous (no auth)

- [FR-USR-001] Can view all tournaments and public data (read-only).
- [FR-USR-002] Cannot see tournament codes or team contact info.

### Authenticated user (JWT)

- [FR-USR-010] Pre-defined in config (`Users:Entries`) with username, password, display name, and role; default role is `User`.
- [FR-USR-011] Can create tournaments and fully manage their own tournaments.
- [FR-USR-012] Can see tournament codes and team contact info for their own tournaments.

### Admin (JWT, role = `Admin`)

- [FR-USR-020] Pre-defined in config like regular users but with `Role = Admin`.
- [FR-USR-021] Can manage (edit, delete) any tournament, not just their own.
- [FR-USR-022] Can see tournament codes and team contact info for all tournaments.
- [FR-USR-023] Treated as "owner" for all authorisation purposes.

### Participant (tournament code)

- [FR-USR-030] Can record game results using the `X-Tournament-Code` header; no JWT required.
