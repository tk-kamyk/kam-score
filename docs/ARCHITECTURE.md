# KamScore Architecture

## System Overview

KamScore is a tournament management system built with a .NET 10 API backend and a Vue 3 SPA frontend, backed by Azure Cosmos DB.

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│  Vue 3 SPA  │────>│  .NET 10 API │────>│  Cosmos DB   │
│  (Vuetify)  │<────│  (REST)      │<────│  (NoSQL)     │
└─────────────┘     └──────────────┘     └──────────────┘
```

In Docker, nginx serves the SPA and proxies `/api/` requests to the backend.

## Backend Architecture

The backend follows **Clean Architecture** with four layers:

```
Domain              Pure C# — entities, value objects, enums, domain logic
  ^
Application         Service interfaces, DTOs, validators, exception types
  ^
Infrastructure      Cosmos DB repositories, JWT auth service, DI wiring
  ^
Api                 ASP.NET Core minimal API endpoints, middleware, Program.cs
```

Dependencies point inward. Domain has no external dependencies. Application defines interfaces that Infrastructure implements. Api is the composition root.

### Key Design Decisions

**Domain-driven design**: Business logic lives in domain entities via factory methods (`Create`) and mutation methods (`Update`). For simple CRUD, endpoint handlers call the repository directly — no service layer. Services are introduced only when orchestration logic grows complex.

**Base Entity class**: All domain entities inherit from `Entity`, which provides `Id` (string) and `LastModified` (DateTime?) properties.

**Container-per-entity**: Each entity type gets its own Cosmos DB container. Container names are derived by convention from C# types (e.g., `Tournament` → `"tournaments"`, `Team` → `"teams"`) via `CosmosRepository<T>.GetContainerName()`. Partition keys are entity-specific: `/ownerId` for tournaments, `/tournamentId` for teams.

**Options pattern**: All configuration is bound to strongly-typed classes (`JwtOptions`, `UserOptions`, `CosmosDbOptions`, `CorsOptions`).

## Authentication

Authentication uses **config-based JWT tokens** — a deliberate choice over Azure Entra ID for simplicity, given the system serves 1-3 tournament organizers.

### Three-Tier Access Model

| Tier | Mechanism | Permissions |
|------|-----------|-------------|
| Owner | `Authorization: Bearer <JWT>` | Full CRUD on own tournaments |
| Participant | `X-Tournament-Code: <code>` header | Record game results only |
| Anonymous | No auth | Read-only, tournament code hidden |

### Login Flow

1. `POST /api/auth/login` with `{ username, password }`
2. Server validates against users defined in `appsettings.json` (`Users:Entries`)
3. Returns JWT token (HMAC-SHA256 signed) with `ClaimTypes.NameIdentifier` = username
4. SPA stores token in Pinia auth store and attaches it as `Authorization: Bearer` header
5. `.RequireAuthorization()` on minimal API endpoint groups enforces authentication
6. `CurrentUserService` extracts user identity from claims
7. Endpoint handler verifies ownership via domain entity (`tournament.IsOwnedBy(userId)`)

### Configuration

```json
{
  "Jwt": {
    "Secret": "<32+ character secret>",
    "Issuer": "KamScore",
    "Audience": "KamScore",
    "ExpirationMinutes": 480
  },
  "Users": {
    "Entries": [
      { "Username": "admin", "Password": "admin123", "DisplayName": "Administrator" }
    ]
  }
}
```

In production, `Jwt:Secret` and user passwords should be set via environment variables or Azure Key Vault.

## Frontend Architecture

The SPA is built with **Vue 3** (Composition API), **Vuetify 4** for UI components, **Pinia** for state management, and **Axios** for HTTP requests.

### Key Components

- **Auth store** (`stores/auth.ts`): Manages JWT token, login dialog state, and API authorization header
- **API client** (`api/client.ts`): Axios instance with base URL `/api` and 401 response interceptor for auto-logout
- **LoginDialog** (`components/LoginDialog.vue`): Modal with username/password fields
- **Tournament views**: List, detail, and participant views with tabbed sub-sections (teams, courts, structure, games, schedule)

### PWA Support

The SPA is configured as a Progressive Web App using `vite-plugin-pwa`, enabling offline-capable installation.

## Docker Deployment

`docker-compose.yml` defines two services:

| Service | Image | Port |
|---------|-------|------|
| `api` | .NET 10 (multi-stage build) | 5001 -> 8080 |
| `spa` | nginx serving Vue build | 3000 -> 80 |

The SPA's nginx config proxies `/api/` to the API container. The API connects to Azure Cosmos DB using the connection string from environment configuration.

## Error Handling

The API uses `ExceptionHandlingMiddleware` to convert domain exceptions to RFC 7807 Problem Details:

| Exception | HTTP Status |
|-----------|-------------|
| `NotFoundException` | 404 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ValidationException` | 400 |
| Unhandled | 500 |
