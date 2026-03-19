# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KamScore is a tournament management system for organizing sports tournaments (Volleyball, Beach Volleyball) with flexible structures, scheduling, and live result entry. It supports multi-phase tournaments with round-robin and play-off formats.

## Requirements

The requirements are in `/docs/requirements/` (area-specific markdown files).
BDD specifications are in `/docs/bdd/` (Gherkin `.feature` files).

## Planning Mode - IMPORTANT

For each iteration:
- Check the requirements
- Ignore the requirements under the `TBC` header
- Transform requirements into BDD specs in `/docs/bdd/` if necessary
- Write tests first
- Implement the functionality
- Verify all tests pass
- If there are any corrections, extend the CLAUDE.md 'Coding Standards' section with the correction
- If there are changes to the requirements based on the chat, update them

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

## Technology Stack (Decided)

- **.NET 10** with implicit usings, nullable reference types, `.slnx` solution format
- **Vue 3** + **TypeScript** + **Vuetify 4** + **Pinia** + **Vite** for the SPA
- **Azure Cosmos DB**
- **Config-based JWT** authentication (users defined in appsettings)
- **xUnit** + **FluentAssertions** + **FakeItEasy** for testing
- **AutoMapper** for entity↔DTO mapping
- **FluentValidation** for input validation
- **Swashbuckle 10.x** for Swagger/OpenAPI/Swagger UI (with JWT Bearer security)
- **Docker** + **docker-compose** for local development
- **Azure** for production hosting (with Terraform/Bicep as IaC)
- **GitHub** as code repository with **GitHub Actions** for CI/CD
- **Application Insights** for logging/telemetry

## Architecture

### Clean Architecture Layers

```
Domain (no dependencies)
  <- Application (depends on Domain)
    <- Infrastructure (depends on Application)
       Api (depends on Infrastructure + Application)
```

### Data Storage

Azure Cosmos DB with dedicated containers per entity

### Authentication (Three-Tier Access)

1. **Owner** (JWT token): Full CRUD on own tournaments, sees tournament code
2. **Participant** (tournament code via `X-Tournament-Code` header): Can record game results only
3. **Anonymous**: Read-only access, tournament code hidden

Login: `POST /api/auth/login` with username/password from config, returns JWT token.
Users are defined via User Secrets (local dev) or environment variables (Docker). See **Local Development Setup** below.

### API Endpoints

| Endpoint Group | Routes | Auth |
|----------------|--------|------|
| Auth | `POST /api/auth/login` | Public |
| Tournaments | `GET/POST/PUT/DELETE /api/tournaments` | GET public, mutations require JWT |
| Teams | `/api/tournaments/{id}/teams` | GET public, mutations require JWT |
| Courts | `/api/tournaments/{id}/courts` | GET public, mutations require JWT |
| Phases | `/api/tournaments/{id}/phases` | GET public, mutations require JWT |
| Games | `/api/tournaments/{id}/games` | GET public, mutations require JWT |
| Schedule | `/api/tournaments/{id}/schedule` | GET public, mutations require JWT |
| Health | `GET /api/health` | Public |

### Containers

Each part of the system (API, frontend) should have its own Docker file.
The root of the application should have a docker-compose file that spins both frontend and backend.
That file is only used for development on local machines.
For production deployment only the Docker files will be used.

## Security

- JWT Bearer authentication with HMAC-SHA256 signed tokens
- Token configuration in `Jwt` section (Secret, Issuer, Audience, ExpirationMinutes)
- Swagger UI includes "Authorize" button for testing with Bearer tokens
- CORS configured per environment via `Cors:AllowedOrigins`
- Owner verification on all mutating operations (`tournament.OwnerId == currentUser.UserId`)

## Local Development Setup

Secrets are **not** stored in appsettings files. Use one of these approaches:

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

Copy `.env.example` to `.env` and fill in your values. Then run `docker compose up`.

## Key Commands

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

## Naming Conventions

- **Projects**: `KamSquare.KamScore.{Layer}`
- **Test projects**: `KamSquare.KamScore.{Layer}.{UnitTest|IntegrationTest}`
- **Configuration**: `appsettings.json` + `appsettings.Development.json`
- **Environment variables**: Double-underscore for nested keys (e.g., `Jwt__Secret`)

## Known Technical Notes

- .NET 10 uses `.slnx` format, not `.sln`
- Swashbuckle 10.x uses Microsoft.OpenApi v2 — types are in `Microsoft.OpenApi` namespace (not `Microsoft.OpenApi.Models`)
- Infrastructure.csproj requires `<FrameworkReference Include="Microsoft.AspNetCore.App" />`

## Code Standards

### Established patterns
- **Clean Architecture** with Dependency Injection throughout
- **Domain objects** pattern (logic lives in domain entities)
- **Options Pattern** for all configuration (`JwtOptions`, `UserOptions`, `CosmosDbOptions`, `CorsOptions`)
- **FluentValidation** for request DTOs (auto-discovered from Application assembly)
- **Mapper Pattern** for DTO transformations
- **4-space indentation** with Allman brace style
- **Implicit type declarations** (`var`)
- **Record types** for DTOs
- **Comprehensive logging** via Application Insights

### Corrections and details - API
- Prefer DDD with logic in entities/domain models
- Avoid large service classes
- Use records for value objects, DTOs, and simple data structures. Prefer records over classes/tuples whenever a type carries data with no mutable behavior
- Prefer modern .NET design, e.g. with minimal APIs
- Use testcontainers and mocking for testing, avoid creating 'InMemory' equivalents of existing classes
- **AutoMapper** for entity↔DTO mapping (registered via assembly scanning)
- **FakeItEasy** for mocking (not Moq, not InMemory equivalents)
- **Single DTO per entity** for create, update, and response (nullable fields for response-only data like Id, OwnerId)
- **No service layer for simple CRUD** — endpoint handlers call repository directly; introduce services only when orchestration logic grows
- **Minimal API endpoint groups** — static classes with `Map*Endpoints()` extension methods
- **Public setters on domain entities** are accepted for Cosmos DB serialization compatibility; domain logic is enforced through factory methods (`Create`) and mutation methods (`Update`)

#### Domain logic placement
- Validation, format-checking, and business rules that operate on a single entity belong **in the entity class**, not in API helpers or endpoints. Example: tournament code validation (`IsCodeValid()`) lives on `Tournament`, not in an endpoint helper
- **Endpoint handlers must NOT contain business rules** — including field classification (e.g., which fields are "structural"), conditional business logic, or domain calculations. Move these to entity methods or domain services. Endpoints should only: validate auth, map DTOs, delegate to domain/services, return HTTP responses
- Use `[GeneratedRegex]` with `partial class` when entities need regex validation
- Domain services (static classes in `Domain/Services/`) handle cross-entity logic that doesn't belong to a single entity (e.g., `GameScheduler`, `PhaseAdvancementCalculator`)
- **Phase format strategy pattern**: All format-specific logic (game generation, standings calculation, validation, cross-group ranking) lives in `IPhaseFormatStrategy` implementations under `Domain/Services/Formats/`. New formats require only: (1) new `PhaseFormat` enum value, (2) new strategy class, (3) new case in `PhaseFormatStrategy.For()`. `StandingsCalculator` is a thin facade that delegates to strategies. `BracketUtilities` and `BracketStandingsHelper` provide shared bracket logic for strategies to reuse

#### Control flow
- **Prefer early returns (guard clauses)** over nested if/else blocks. Handle the simple/default case first and return, then continue with the main logic at the top indentation level

#### Async patterns
- **Never use `.Result`** on tasks, even after `Task.WhenAll`. Always `await` — `.Result` can deadlock and hides exceptions inside `AggregateException`
- Prefer `await Task.WhenAll(...)` for concurrent independent operations, then `await` each task individually to unwrap results

#### Service extraction criteria
- Extract an Application service when an endpoint handler exceeds simple CRUD: multi-repository coordination, conditional logic across entities, or reuse across multiple endpoints
- `PhaseCompletionService` owns all phase lifecycle operations (complete, reopen, placeholder creation/regeneration)
- `ScheduleGenerationService` owns game generation + scheduling orchestration
- Keep services focused — one cohesive responsibility per service, not a catch-all

#### FluentValidation rules
- When a DTO has **mutually exclusive fields** (e.g., `Sets` vs `HomeScore`/`AwayScore`), add an explicit rejection rule before the "at least one required" rule
- Order validation rules: mutual exclusivity → presence → format → business rules

#### Repository query design
- **Push filtering to the database.** Do not fetch all records and filter in-memory when Cosmos DB supports WHERE clauses. Use parameterized `QueryDefinition` with dynamic condition building
- Repository methods that accept optional filter parameters (e.g., `GetGamesAsync(tournamentId, phaseId?, groupId?, courtId?)`) should build WHERE clauses conditionally
- Always use `@parameterName` placeholders — never interpolate values into query strings

#### Loop safety
- **Unbounded loops are prohibited.** Any `for(;;)` or `while(true)` must have a safety limit (e.g., `maxSlotLimit = items.Count * 10`). Throw `InvalidOperationException` if the limit is reached
- Extract complex loop bodies into `Try*` methods returning `bool` for clarity

### Corrections and details - Frontend
- Prefer Vue3 patterns with single file components
- Group SPA code by domain (e.g., `auth/`, `tournament/`), not by layer. Each domain folder contains its store, types, and components. The `views/` folder contains thin route wrappers that import and render domain components
- Keep shared infrastructure (`api/client.ts`, `composables/`, `router/`) separate from domain folders
- **Responsive sizing**: Prefer Vuetify 4 responsive utility classes (e.g., `text-body-small text-md-body-medium`, `px-3 px-lg-10`) over custom CSS media queries. Use `@media` only for properties that have no Vuetify utility equivalent (e.g., `letter-spacing`, `max-width`, combined multi-property overrides)
- **Responsive layout**: Use Vuetify's responsive flex utility classes (`d-flex`, `flex-sm-row`, `flex-wrap`, `ga-4`) for responsive layouts instead of custom CSS grid or media queries. For equal-width children that grow to fill available space, combine flex utilities with `flex: 1 1 0` on children
- **CSS Cascade Layers**: Custom CSS that targets Vuetify internal classes (`.v-btn--*`, `.v-card-title`, `.v-table`, etc.) must be wrapped in `@layer overrides { ... }` to work correctly with Vuetify 4's cascade layer system. Non-Vuetify utility classes and `:root`/`body` styles stay outside layers. The layer order declaration (`@layer vuetify-core, vuetify-components, ..., overrides;`) lives in `index.html` as an inline `<style>` to guarantee it precedes Vite's bundled CSS
- **Thin components**: Extract presentational sub-components when a parent exceeds ~150 lines or when template blocks are reused across files. Child components own their helpers and scoped styles; parents own orchestration (store calls, dialogs, route state)