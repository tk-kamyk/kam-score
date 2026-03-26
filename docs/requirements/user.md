# User

## Permission Tiers

### Anonymous (no auth)
- Can view all tournaments and public data (read-only)
- Cannot see tournament codes or team contact info

### Authenticated User (JWT)
- Pre-defined in config (`Users:Entries`) with username, password, display name, and role
- Default role is `User`
- Can create tournaments and fully manage their own tournaments
- Can see tournament codes and team contact info for their own tournaments

### Admin (JWT, role = `Admin`)
- Pre-defined in config like regular users, but with `Role = Admin`
- Can manage (edit, delete) any tournament, not just their own
- Can see tournament codes and team contact info for all tournaments
- Treated as "owner" for all authorization purposes

### Participant (tournament code)
- Can record game results using `X-Tournament-Code` header
- No JWT required