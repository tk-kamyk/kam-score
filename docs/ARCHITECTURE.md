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

**Base Entity class**: All domain entities inherit from `Entity`, which provides `Id` (string), `LastModified` (DateTime?), and `ETag` (string?) properties. ETag supports optimistic concurrency via Cosmos DB's `If-Match` header.

**Container-per-entity**: Each entity type gets its own Cosmos DB container. Container names are derived by convention from C# types (e.g., `Tournament` → `"tournaments"`, `Team` → `"teams"`) via `CosmosRepository<T>.GetContainerName()`. Partition keys are entity-specific: `/ownerId` for tournaments, `/tournamentId` for teams.

**Options pattern**: All configuration is bound to strongly-typed classes (`JwtOptions`, `UserOptions`, `CosmosDbOptions`, `CorsOptions`).

### Where Logic Lives

The codebase follows a strict placement hierarchy for business logic:

| Logic type | Location | Example |
|------------|----------|---------|
| Single-entity validation/rules | Entity class (Domain) | `Tournament.IsCodeValid()`, `Tournament.IsOwnedBy()` |
| Single-entity creation/mutation | Entity factory/mutation methods | `Tournament.Create()`, `Game.RecordResult()` |
| Cross-entity calculations | Domain services (static classes) | `StandingsCalculator`, `GameScheduler`, `BracketUtilities` |
| Multi-repository orchestration | Application services | `PhaseCompletionService`, `ScheduleGenerationService` |
| Request validation | FluentValidation validators (Application) | `GameResultDtoValidator`, `TournamentDtoValidator` |
| HTTP/auth concerns | API helpers or endpoint handlers | `TournamentAuthorizationHelper` |
| Simple CRUD wiring | Endpoint handlers directly | `TournamentEndpoints.CreateTournament()` |

**Rule of thumb**: If logic only needs the entity's own state, it belongs on the entity. If it needs multiple repositories or cross-entity coordination, it belongs in an Application service. API-layer code should only contain HTTP-specific concerns (headers, routing, auth checks) and delegation.

**Anti-patterns — do NOT put these in endpoint handlers:**
- Field classification logic (e.g., determining which fields are "structural" vs "non-structural") → entity method
- Conditional business logic (e.g., "if format changed, block update") → entity or guard service
- Domain calculations or comparisons between old and new state → entity method
- Business rule checks beyond simple null/empty guards → entity or domain service

### Application Services

Services are introduced when endpoint handlers grow beyond simple CRUD:

**PhaseCompletionService** — Owns all phase lifecycle operations:
- `CompletePhaseAsync` — validates all games complete, calculates standings, resolves placeholders, advances to next phase
- `ReopenPhaseAsync` — unresolves placeholder teams, resets phase status
- `CreatePlaceholdersForNewPhaseAsync` — auto-creates placeholder teams when a new phase is added after a phase with progression config
- `RegeneratePlaceholdersOnUpdateAsync` — deletes and recreates placeholders when a phase's progression config changes

**ScheduleGenerationService** — Owns game generation + scheduling:
- `GenerateAndScheduleAsync` — generates round-robin/playoff games, assigns referees, schedules across courts

### Domain Services

Static classes in `Domain/Services/` that encapsulate cross-entity logic with no external dependencies:

| Service | Responsibility |
|---------|---------------|
| `StandingsCalculator` | Calculates standings for round-robin, playoff elimination, and playoff with placement formats |
| `GameScheduler` | Schedules games across courts/time slots with conflict avoidance, referee constraints, and court distribution |
| `RoundRobinGenerator` | Generates all games for a round-robin group |
| `PlayoffEliminationGenerator` | Generates bracket games for single-elimination playoffs |
| `PlayoffWithPlacementGenerator` | Generates bracket games with placement matches (3rd place, etc.) |
| `PlaceholderTeamGenerator` | Creates placeholder teams for cross-phase progression |
| `PlaceholderResolver` | Resolves/unresolves placeholder teams to real teams after phase completion |
| `PhaseAdvancementCalculator` | Determines which teams qualify and their seeding |
| `BracketUtilities` | Bracket math (next power of 2, advancement resolution) |

### Repository Query Design

Cosmos DB repositories follow these patterns:

- **Parameterized queries only** — all values passed via `@parameterName` placeholders, never string interpolation
- **Push filtering to the database** — optional filter parameters build dynamic WHERE clauses server-side rather than fetching all records and filtering in-memory. Example: `GetGamesAsync(tournamentId, phaseId?, groupId?, courtId?)`
- **Partition-aware queries** — always include `PartitionKey` in `QueryRequestOptions` to avoid cross-partition queries
- **ETag-based concurrency** — `UpdateAsync` methods pass `IfMatchEtag` when the entity has an ETag, enabling optimistic concurrency

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
