# CLAUDE.md

## MANDATORY: Development Process

**STOP.** For every feature, bug fix, or change — follow these gates IN ORDER.
Do NOT skip gates. Do NOT start coding before Gate 4.
I MUST REFUSE to proceed if a gate is incomplete. No exceptions. No "I'll come back to it."
If the user asks to skip a gate, remind them of this rule and ask which gate output they want to produce first.

### Gate 1: Requirements
- Read relevant `docs/requirements/*.md`
- Ignore anything under a `TBC` header
- If the requirement is missing or unclear, ASK the user
- If chat reveals requirement changes, UPDATE the requirements file

### Gate 2: BDD Specification
- Write/update Gherkin scenarios in `docs/bdd/*.feature`
- Each scenario must map to a testable behavior
- Get user confirmation before proceeding

### Gate 3: Mocked UI (frontend features only — skip for pure backend work)
- Build the Vue component with hardcoded/mock data
- No API calls yet — use static data matching the BDD scenarios
- Show the user for feedback

### Gate 4: Failing Tests
- Write xUnit tests that express the BDD scenarios
- Domain unit tests for business logic
- Integration tests for API endpoints
- ALL tests must FAIL (red) before writing implementation
- Run `dotnet test api/KamScore.slnx` to confirm failures

### Gate 5: API Implementation
- Implement domain logic, services, endpoints
- Run `dotnet test api/KamScore.slnx` — ALL tests must PASS (green)
- If any test fails, fix implementation (not the test) unless the test is wrong

### Gate 6: Connect UI to API (skip if no frontend component)
- Replace mock data with real API calls
- Verify end-to-end flow works
- Run `cd spa && npx vue-tsc --noEmit` for type safety

### Gate 7: Cleanup
- If corrections were needed, add them to Code Standards below
- If requirements changed, verify `docs/` are updated

---

## Project Overview

KamScore is a tournament management system for sports tournaments (Volleyball, Beach Volleyball) with flexible structures, scheduling, and live result entry. Multi-phase tournaments with round-robin and play-off formats.

Requirements: `docs/requirements/*.md` | BDD specs: `docs/bdd/*.feature`

---

## Repository Structure

```
api/                          # .NET 10 backend
  src/
    KamSquare.KamScore.Domain/        # Entities, value objects, enums
    KamSquare.KamScore.Application/   # Services, DTOs, validators, interfaces
    KamSquare.KamScore.Infrastructure/ # Cosmos DB, auth service, DI setup
    KamSquare.KamScore.Api/           # Minimal API endpoints, middleware, Program.cs
  tests/
    KamSquare.KamScore.Domain.UnitTest/
    KamSquare.KamScore.Application.UnitTest/
    KamSquare.KamScore.Api.IntegrationTest/
  KamScore.slnx                # .NET 10 solution file (.slnx format)
  Dockerfile

spa/                          # Vue 3 frontend
  src/
    api/          # Shared Axios client
    auth/         # Auth store, types, components
    tournament/   # Tournament store, types, components
    composables/  # Shared composables (useSnackbar)
    views/        # Thin route views (wrappers only)
    router/       # Vue Router configuration
  Dockerfile
  nginx.conf

docs/
  requirements/   # Feature requirements by area
  bdd/            # Gherkin BDD specifications

docker-compose.yml
```

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10, implicit usings, nullable reference types, `.slnx` solution format |
| Frontend | Vue 3 + TypeScript + Vuetify 4 + Pinia + Vite |
| Database | Azure Cosmos DB (container-per-entity) |
| Auth | Config-based JWT (users in appsettings) |
| Testing | xUnit + FluentAssertions + FakeItEasy |
| Mapping | AutoMapper (assembly scanning) |
| Validation | FluentValidation (auto-discovered from Application assembly) |
| API Docs | Swashbuckle 10.x (JWT Bearer security) |
| Hosting | Docker + docker-compose (dev), Azure (prod) |
| CI/CD | GitHub Actions |
| Telemetry | Application Insights |

---

## Architecture

### Clean Architecture Layers

```
Domain (no dependencies)
  <- Application (depends on Domain)
    <- Infrastructure (depends on Application)
       Api (depends on Infrastructure + Application)
```

Dependencies point inward. Domain has no external dependencies. Application defines interfaces that Infrastructure implements. Api is the composition root.

### Where Logic Lives

| Logic type | Location | Example |
|------------|----------|---------|
| Single-entity validation/rules | Entity class (Domain) | `Tournament.IsCodeValid()`, `Tournament.IsOwnedBy()` |
| Single-entity creation/mutation | Entity factory/mutation methods | `Tournament.Create()`, `Game.RecordResult()` |
| Cross-entity calculations | Domain services (static classes) | `StandingsCalculator`, `GameScheduler`, `BracketUtilities` |
| Multi-repository orchestration | Application services | `PhaseCompletionService`, `ScheduleGenerationService` |
| Request validation | FluentValidation validators (Application) | `GameResultDtoValidator`, `TournamentDtoValidator` |
| HTTP/auth concerns | API helpers or endpoint handlers | `TournamentAuthorizationHelper` |
| Simple CRUD wiring | Endpoint handlers directly | `TournamentEndpoints.CreateTournament()` |

**Rule**: If logic only needs the entity's own state → entity. If it needs multiple repositories → Application service. API layer = HTTP concerns + delegation only.

### Domain Services

Static classes in `Domain/Services/` — cross-entity logic, no external dependencies:

| Service | Responsibility |
|---------|---------------|
| `StandingsCalculator` | Thin facade delegating to format-specific strategies via `PhaseFormatStrategy` |
| `FinalStandingsCalculator` | Cross-phase final standings; delegates cross-group ranking to format strategies |
| `GameScheduler` | Schedules games across courts/time slots with conflict avoidance |
| `PlaceholderTeamGenerator` | Creates placeholder teams for cross-phase progression |
| `PlaceholderResolver` | Resolves/unresolves placeholder teams after phase completion |
| `PhaseAdvancementCalculator` | Determines qualifying teams and seeding |
| `BracketUtilities` | Bracket math (next power of 2, bracket ordering, advancement resolution) |
| `RefereeAssigner` | Assigns referees based on team availability |

### Application Services

Extracted when endpoint handlers grow beyond simple CRUD:

| Service | Responsibility |
|---------|---------------|
| `PhaseCompletionService` | Phase complete/reopen, placeholder creation/regeneration |
| `ScheduleGenerationService` | Game generation + scheduling orchestration |

**Extraction criteria**: multi-repository coordination, conditional logic across entities, or reuse across multiple endpoints.

### Phase Format Strategy Pattern

All format-specific logic lives in `IPhaseFormatStrategy` implementations (`Domain/Services/Formats/`):

```
IPhaseFormatStrategy
├── GenerateGames()         — produces games for a group
├── CalculateStandings()    — computes standings from completed games
├── RankCrossGroup()        — ranks standings across multiple groups
├── SupportsRefereeAssignment — whether the format uses referees
└── ValidateTeams()         — format-specific team count validation
```

Implementations: `RoundRobinStrategy`, `PlayoffEliminationStrategy`, `PlayoffWithPlacementStrategy`, `DoubleEliminationStrategy`, `DoubleEliminationVdStrategy`.

Factory: `PhaseFormatStrategy.For(PhaseFormat)` returns the correct instance.

**Adding a new format**: (1) new `PhaseFormat` enum value, (2) new strategy class, (3) new case in `PhaseFormatStrategy.For()`. No changes to consumers.

### Error Handling

`ExceptionHandlingMiddleware` converts domain exceptions to RFC 7807 Problem Details:

| Exception | HTTP Status |
|-----------|-------------|
| `NotFoundException` | 404 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ValidationException` | 400 |
| Unhandled | 500 |

---

## Authentication

Config-based JWT — deliberate choice over Azure Entra ID for simplicity (1-3 tournament organizers).

### Three-Tier Access

| Tier | Mechanism | Permissions |
|------|-----------|-------------|
| Owner | `Authorization: Bearer <JWT>` | Full CRUD on own tournaments |
| Participant | `X-Tournament-Code: <code>` header | Record game results only |
| Anonymous | No auth | Read-only, tournament code hidden |

### API Endpoints

| Group | Routes | Auth |
|-------|--------|------|
| Auth | `POST /api/auth/login` | Public |
| Tournaments | `GET/POST/PUT/DELETE /api/tournaments` | GET public, mutations JWT |
| Teams | `/api/tournaments/{id}/teams` | GET public, mutations JWT |
| Courts | `/api/tournaments/{id}/courts` | GET public, mutations JWT |
| Phases | `/api/tournaments/{id}/phases` | GET public, mutations JWT |
| Games | `/api/tournaments/{id}/games` | GET public, mutations JWT |
| Schedule | `/api/tournaments/{id}/schedule` | GET public, mutations JWT |
| Health | `GET /api/health` | Public |

### Security Details
- JWT Bearer with HMAC-SHA256, config in `Jwt` section (Secret, Issuer, Audience, ExpirationMinutes)
- CORS via `Cors:AllowedOrigins`
- Owner verification: `tournament.IsOwnedBy(userId)` on all mutations
- Contact info (email, phone) on teams hidden from non-owners

---

## Commands

```bash
# Backend
dotnet build api/KamScore.slnx
dotnet test api/KamScore.slnx
dotnet run --project api/src/KamSquare.KamScore.Api

# Frontend
cd spa && npm install
cd spa && npm run dev          # Dev server at localhost:5173
cd spa && npx vite build       # Production build
cd spa && npx vue-tsc --noEmit # Type checking

# Docker
docker compose up              # Full stack (API + SPA)
```

---

## Local Development Setup

Secrets are **not** stored in appsettings files.

### Option A: .NET User Secrets (for `dotnet run`)

```bash
cd api/src/KamSquare.KamScore.Api
dotnet user-secrets set "Jwt:Secret" "64-character-long-secret"
dotnet user-secrets set "Users:Entries:0:Username" "username"
dotnet user-secrets set "Users:Entries:0:Password" "password"
dotnet user-secrets set "Users:Entries:0:DisplayName" "DisplayName"
dotnet user-secrets set "CosmosDb:ConnectionString" "your-connection-string"
```

### Option B: Docker Compose (uses `.env` file)

Copy `.env.example` to `.env` and fill in values. Then `docker compose up`.

---

## Code Standards

### General patterns
- **Clean Architecture** with Dependency Injection
- **DDD** — logic lives in domain entities
- **Options Pattern** for all configuration (`JwtOptions`, `UserOptions`, `CosmosDbOptions`, `CorsOptions`)
- **4-space indentation**, Allman brace style, `var` for type declarations
- **Record types** for DTOs, value objects, and simple data structures

### API standards

| Rule | Detail |
|------|--------|
| Minimal API | Static classes with `Map*Endpoints()` extension methods |
| Single DTO per entity | Nullable fields for response-only data (Id, OwnerId) |
| No service layer for CRUD | Endpoint handlers call repository directly |
| AutoMapper | Entity↔DTO mapping via assembly scanning |
| FakeItEasy | For all mocking (not Moq, not InMemory equivalents) |
| FluentValidation | Auto-discovered from Application assembly |
| Public setters on entities | For Cosmos DB serialization; logic enforced via `Create`/`Update` methods |
| `[GeneratedRegex]` | With `partial class` when entities need regex validation |

### Domain logic placement
- Single-entity rules → entity class (e.g., `Tournament.IsCodeValid()`)
- Cross-entity logic → Domain services (static classes in `Domain/Services/`)
- Multi-repo orchestration → Application services

### ANTI-PATTERNS — do NOT put these in endpoint handlers
- Field classification logic (e.g., which fields are "structural")
- Conditional business logic (e.g., "if format changed, block update")
- Domain calculations or old/new state comparisons
- Business rule checks beyond simple null/empty guards

Endpoints should ONLY: validate auth, map DTOs, delegate to domain/services, return HTTP responses.

### Control flow
- **Early returns (guard clauses)** over nested if/else
- Handle simple/default case first, return, then main logic at top indentation

### Async patterns
- **Never `.Result`** on tasks — always `await` (deadlock risk, hides exceptions)
- `await Task.WhenAll(...)` for concurrent operations, then `await` each individually

### FluentValidation rules
- Order: mutual exclusivity → presence → format → business rules
- Explicit rejection for mutually exclusive fields before "at least one required"

### Repository query design
- **Push filtering to database** — no fetch-all + in-memory filter
- Build WHERE clauses conditionally from optional parameters
- Always `@parameterName` placeholders — never string interpolation
- Include `PartitionKey` in `QueryRequestOptions`
- ETag-based optimistic concurrency via `If-Match`

### Loop safety
- **Unbounded loops prohibited** — safety limit on `for(;;)`/`while(true)` (e.g., `items.Count * 10`)
- Throw `InvalidOperationException` if limit reached
- Extract complex loop bodies into `Try*` methods returning `bool`

### Service extraction criteria
- Extract when: multi-repo coordination, conditional cross-entity logic, reuse across endpoints
- Keep services focused — one cohesive responsibility per service

### Frontend standards
- **Vue 3** Composition API, single file components
- **Group by domain** (`auth/`, `tournament/`), not by layer
- Shared infra (`api/client.ts`, `composables/`, `router/`) separate from domain folders
- `views/` = thin route wrappers only
- **Responsive sizing**: Vuetify 4 responsive utility classes over custom CSS media queries
- **Responsive layout**: Vuetify flex utilities (`d-flex`, `flex-sm-row`, `flex-wrap`, `ga-4`)
- **CSS Cascade Layers**: Custom CSS targeting Vuetify internals must use `@layer overrides { ... }`
- Layer order declaration in `index.html` inline `<style>`
- **Thin components**: Extract sub-components when parent exceeds ~150 lines or template blocks are reused

---

## Known Technical Gotchas

- .NET 10 uses `.slnx` format, not `.sln`
- Swashbuckle 10.x uses Microsoft.OpenApi v2 — types in `Microsoft.OpenApi` namespace (NOT `Microsoft.OpenApi.Models`)
- Infrastructure.csproj requires `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- Docker: nginx serves SPA and proxies `/api/` to backend (ports: API 5001→8080, SPA 3000→80)

---

## Naming Conventions

- **Projects**: `KamSquare.KamScore.{Layer}`
- **Test projects**: `KamSquare.KamScore.{Layer}.{UnitTest|IntegrationTest}`
- **Config**: `appsettings.json` + `appsettings.Development.json`
- **Env vars**: Double-underscore for nested keys (e.g., `Jwt__Secret`)
