# CLAUDE.md

KamScore is a tournament management system for volleyball / beach volleyball — multi-phase tournaments with flexible structures, scheduling, and live result entry. Owners run their events; participants record results via a shared code; everyone else gets read-only views.

Process, conventions, and stack craft live under `.claude/`. See `.claude/CLAUDE.md` for the toolbox map, `.claude/rules/delegation-map.md` for routing, and `.claude/rules/project-glossary.md` for the ubiquitous language.

## Agent team and entry point

Substantive work routes through **`/agentic-dev-team:orchestrator`** (bare `/orchestrator` is a project-local alias). The seven-gate pipeline is enforced by the `generic-gate-pipeline` skill; the substantive-vs-trivial threshold by `generic-orchestrator-routing`.

Do **not** use the `nw:*` skills (`nw:discuss`, `nw:design`, `nw:deliver`, …) — they're a parallel agentic harness that ships installed but is **not used here**. They break dispatch for this project; always go through the `agentic-dev-team` plugin.

## Stack manifest (single source of truth)

```yaml
# stack-manifest
stacks:
  generic: { enabled: true }
  dotnet:  { enabled: true, root: api/, test_cmd: "dotnet test api/KamScore.slnx" }
  vue:     { enabled: true, root: spa/, test_cmd: "cd spa && npm run build" }
```

The orchestrator filters skills whose `metadata.stack` ⊄ enabled stacks. The `guard-stack-manifest.sh` hook validates this block on every CLAUDE.md edit.

---

## Requirements & design docs

Requirements split into focused per-area files. Implementation-level detail (formulas, exact game sequences, algorithms) lives in an optional **paired design doc** with the **identical basename** under `docs/design/`.

| Area | Requirements | Paired design |
|---|---|---|
| Structure (phases, groups, overview) | `docs/requirements/structure.md` | — |
| Game generation + scheduling + referee | `docs/requirements/game-generation.md` | `docs/design/game-generation.md` |
| Results, standings, bracket advancement | `docs/requirements/results-and-standings.md` | `docs/design/results-and-standings.md` |
| Phase status, progression, placeholders | `docs/requirements/phase-advancement.md` | `docs/design/phase-advancement.md` |
| Restriction matrix by phase state | `docs/requirements/phase-state-restrictions.md` | — |
| Levels (per-phase divisions) | `docs/requirements/levels.md` | `docs/design/levels.md` |
| Volunteer (entity, shifts, assignment) | `docs/requirements/volunteer.md` | `docs/design/volunteer.md` |
| Tournament, team, court, user, feature-flags | `docs/requirements/<area>.md` | as needed |

**Rule**: a requirement file states *what* the user can do. A design file states *how* we do it. Formulas, named constants (`bracketSize / 2^round + 1`), exact bracket walk orders, and step-by-step algorithms must not appear in the requirements — move them to the paired design doc and link with a `> See design: ./design/<name>.md` line at the top.

Placement heuristic, writing order, and ID conventions live in the `generic-docs-standards` and `generic-spec-authoring` skills.

---

## Repository structure

```
api/                          # .NET 10 backend
  src/
    KamSquare.KamScore.Domain/        # Entities, value objects, enums, Domain services
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
    api/          # Shared Axios client + error helpers
    auth/         # Auth store, types, components
    tournament/   # Tournament store, types, components
    team/ court/ game/ structure/ standings/ volunteer/    # Domain folders
    composables/  # Shared composables (useSnackbar, useFormErrors, useFeatureFlags, ...)
    components/   # Cross-domain shared UI (ConfirmDialog, ConfirmDeleteDialog, ...)
    views/        # Thin route wrappers only
    router/       # Vue Router configuration
  Dockerfile
  nginx.conf

docs/
  requirements/   # Feature requirements by area (user-intent level)
  design/         # Paired implementation-detail docs (formulas, algorithms)
  bdd/            # Gherkin BDD specifications (behavioural scenarios)

docker-compose.yml
```

---

## Technology stack

| Layer | Technology |
|---|---|
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

Clean Architecture, dependencies point inward:

```
Domain (no dependencies)
  <- Application (depends on Domain)
    <- Infrastructure (depends on Application)
       Api (depends on Infrastructure + Application)
```

Layer responsibilities + how-to-decide-where-logic-goes are in the `dotnet-clean-architecture` skill. Below are the **KamScore-specific** decisions.

### Where logic lives (KamScore inventory)

| Logic type | Location | Example |
|---|---|---|
| Single-entity validation/rules | Entity class (Domain) | `Tournament.IsCodeValid()`, `Tournament.IsOwnedBy()` |
| Single-entity creation/mutation | Entity factory/mutation methods | `Tournament.Create()`, `Game.RecordResult()` |
| Format-specific generation/standings | Strategy + static Generator/Ranker (Domain) | `RoundRobinGenerator`, `PlayoffEliminationStandingsRanker` |
| Cross-entity calculations | Domain services (static classes) | `GameScheduler`, `RefereeAssigner`, `PhaseAdvancementCalculator`, `BracketUtilities` |
| Multi-repository orchestration | Application services | `PhaseCompletionService`, `ScheduleGenerationService`, `BracketAdvancementService`, `VolunteerService`, `TournamentCopyService` |
| Request validation | FluentValidation validators (Application) | `GameResultDtoValidator`, `TournamentDtoValidator` |
| HTTP/auth concerns | API helpers or endpoint handlers | `TournamentAuthorizationHelper` |
| Simple CRUD wiring | Endpoint handlers directly | `TournamentEndpoints.CreateTournament()` |

### Domain services

Static classes in `Domain/Services/` — cross-entity logic, no external dependencies:

| Service | Responsibility |
|---|---|
| `StandingsCalculator` | Thin facade delegating to format-specific `*StandingsRanker` via `PhaseFormatStrategy` |
| `GameScheduler` | Schedules games across courts/time slots with conflict avoidance |
| `PlaceholderTeamGenerator` | Creates placeholder teams for cross-phase progression |
| `PlaceholderResolver` | Resolves/unresolves placeholder teams after phase completion |
| `PhaseAdvancementCalculator` | Determines qualifying teams and seeding |
| `BracketUtilities` | Bracket math (next power of 2, bracket ordering, within-phase advancement resolution) |
| `RefereeAssigner` | Assigns referees based on team availability |
| `VolunteerShiftCalculator` | Computes shift groups/slots for a tournament |

### Application services

Extract when an endpoint handler grows beyond simple CRUD.

| Service | Responsibility |
|---|---|
| `PhaseCompletionService` | Phase complete/reopen/delete-games, placeholder creation/regeneration/resolution |
| `ScheduleGenerationService` | Game generation + scheduling orchestration, phase activation |
| `BracketAdvancementService` | Propagates bracket placeholder resolution after a playoff result is recorded |
| `VolunteerService` | Volunteer CRUD + shift calculation + assignment validation + bulk shift-group clear / auto-assign |
| `TournamentCopyService` | Copy tournament structure from an existing tournament |

### Phase Format Strategy pattern

All format-specific logic lives in `Domain/Services/Formats/` and follows a **uniform three-file layout** per format `Xxx`:

| File | Role |
|---|---|
| `XxxStrategy.cs` | Implements `IPhaseFormatStrategy`; thin orchestrator that delegates to the Generator and StandingsRanker |
| `XxxGenerator.cs` | Static class; owns `Generate()` — bracket/pairing construction, including team-count validation |
| `XxxStandingsRanker.cs` | Static class; owns `Calculate()` (per-group standings) and `RankCrossGroup()` |

Formats: `RoundRobin`, `PlayoffElimination`, `PlayoffWithPlacement`, `DoubleElimination`, `DoubleEliminationVd`.

```
IPhaseFormatStrategy
├── GenerateGames()                 → delegates to XxxGenerator.Generate(...)
├── CalculateStandings()            → delegates to XxxStandingsRanker.Calculate(...)
├── RankCrossGroup()                → delegates to XxxStandingsRanker.RankCrossGroup(...)
├── SupportsRefereeAssignment       (property)
```

Factory: `PhaseFormatStrategy.For(PhaseFormat)` returns the correct instance.

**Adding a new format**:
1. New `PhaseFormat` enum value
2. New `XxxStrategy.cs` + `XxxGenerator.cs` + `XxxStandingsRanker.cs` triad
3. New case in `PhaseFormatStrategy.For()`
4. Test files: `XxxGeneratorTests.cs` (+ ranker test file if non-trivial) in `Domain.UnitTest`

No changes to consumers.

### Error handling

`ExceptionHandlingMiddleware` converts domain exceptions to RFC 7807 Problem Details:

| Exception | HTTP Status |
|---|---|
| `NotFoundException` | 404 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ValidationException` | 400 |
| `PhaseStateException` | 409 |
| Unhandled | 500 |

---

## Authentication

Config-based JWT — a deliberate choice over Azure Entra ID for simplicity (1–3 tournament organizers).

### Three-tier access

| Tier | Mechanism | Permissions |
|---|---|---|
| Owner | `Authorization: Bearer <JWT>` | Full CRUD on own tournaments |
| Participant | `X-Tournament-Code: <code>` header | Record game results only |
| Anonymous | No auth | Read-only, tournament code hidden |

### API endpoints

| Group | Routes | Auth |
|---|---|---|
| Auth | `POST /api/auth/login` | Public |
| Tournaments | `GET/POST/PUT/DELETE /api/tournaments` | GET public, mutations JWT |
| Teams | `/api/tournaments/{id}/teams` | GET public, mutations JWT |
| Courts | `/api/tournaments/{id}/courts` | GET public, mutations JWT |
| Phases | `/api/tournaments/{id}/structure/phases` | GET public, mutations JWT |
| Games | `/api/tournaments/{id}/games` | GET public, mutations JWT (result recording also accepts tournament code) |
| Standings / Final | `/api/tournaments/{id}/standings`, `/final-standings` | Public |
| Volunteers + shifts | `/api/tournaments/{id}/volunteers/**` | All endpoints JWT + owner/admin |
| Feature Flags | `GET /api/feature-flags` | Public (boilerplate — see "Feature flags") |
| Health | `GET /api/health` | Public |

### Security details

- JWT Bearer with HMAC-SHA256; config in `Jwt` section (Secret, Issuer, Audience, ExpirationMinutes).
- CORS via `Cors:AllowedOrigins`.
- Owner verification: `tournament.IsOwnedBy(userId)` on all mutations.
- Contact info (email, phone) on teams hidden from non-owners.

---

## Feature flags

The feature-flag *mechanism* is retained as intentional boilerplate — today there are **no active flags**. Keep the plumbing in place so a new in-development feature can be gated behind a flag without re-introducing the infrastructure each time.

- Backend endpoint: `GET /api/feature-flags` (unauthenticated)
- Frontend composable: `useFeatureFlags()` with `isEnabled(flag)` and session-scoped cache
- Requirements doc: `docs/requirements/feature-flags.md`

**Don't remove** the endpoint / composable / tests — they are marked as intentional boilerplate in their file headers.

---

## Commands

```bash
# Backend
dotnet build api/KamScore.slnx
dotnet test api/KamScore.slnx
dotnet run --project api/src/KamSquare.KamScore.Api

# Frontend
cd spa && npm install
cd spa && npm run dev            # Dev server at localhost:5173
cd spa && npm run lint           # ESLint --fix
cd spa && npm run format         # Prettier
cd spa && npx vue-tsc --noEmit   # Type checking
cd spa && npm run build          # Format + lint + type-check + vite build

# Docker
docker compose up                # Full stack (API + SPA)
```

---

## Local development setup

Secrets are **not** stored in appsettings files.

### Option A: .NET User Secrets (for `dotnet run`)

```bash
cd api/src/KamSquare.KamScore.Api
dotnet user-secrets set "Jwt:Secret" "64-character-long-secret"
dotnet user-secrets set "Users" '[{"username":"username","password":"password","displayName":"DisplayName","role":"User"}]'
dotnet user-secrets set "CosmosDb:ConnectionString" "your-connection-string"
```

### Option B: Docker Compose (uses `.env` file)

Copy `.env.example` to `.env` and fill in values. Then `docker compose up`.

---

## KamScore-specific code patterns

Generic .NET conventions (Clean Arch, DDD, Options Pattern, async, control flow, loop safety, Repository query design, FluentValidation rule ordering) live in `dotnet-coding-patterns`, `dotnet-clean-architecture`, `dotnet-api-design`, `dotnet-data-access`. Vue/Vuetify conventions live in `vue-frontend-standards`. Below are the rules that are **specific to KamScore**.

### Endpoint anti-patterns (push logic down)

Endpoints are thin controllers. Push logic down if you see any of these in an endpoint handler:

- **Field classification** (e.g., deciding which DTO fields are "structural")
- **Conditional business logic** (e.g., "if format changed, block update")
- **Cross-entity coordination** (e.g., fetching games + structure + teams to compute a transition)
- **Cascading state changes** (e.g., `if (phase.Status is Scheduled or InProgress) structure.ResetPhase(...)` — belongs in an Application service)
- **Multi-repo writes that must succeed together** — wrap in a service method so the intent is named
- **Old/new state comparisons** for business decisions
- Business rule checks beyond simple null/empty guards

Endpoints should ONLY: validate auth, map DTOs, call validator, delegate to domain/service, map response, return HTTP.

**Canonical service methods** (extracted because the endpoint was doing too much):

- `PhaseCompletionService.DeletePhaseGamesAsync` — owns "games deleted → phase resets to New"
- `BracketAdvancementService.ResolveAsync` — owns "playoff result recorded → downstream placeholders resolve"
- `VolunteerService.AssignToShiftAsync` — owns "validate shift time is a real slot, assign, persist"

### Service extraction criteria

Extract an Application service when ANY of: multi-repo coordination, conditional cross-entity logic, cascading state changes, reuse across endpoints. Keep services focused — one cohesive responsibility per service. Register as `AddScoped` in `ServiceCollectionExtensions.cs`.

### File-size guidelines

- Domain strategy files: prefer the three-file triad over a monolith — each file under ~400 lines.
- Endpoint files: under ~200 lines; push logic into Application services otherwise.
- Vue single-file components: under ~250 lines; extract sub-components (dialogs, list rows, filter bars) otherwise.

---

## Testing

Generic test philosophy + layer placement is in `generic-code-quality`. Below is the **KamScore** layout and policy.

### Test layout

| Project | Scope |
|---|---|
| `Domain.UnitTest` | Entities, Domain services (including `XxxGeneratorTests`, `XxxStandingsRankerTests`), value objects. Pure, no mocks |
| `Application.UnitTest` | Validators, mappers, Application services. Repositories are FakeItEasy fakes |
| `Api.IntegrationTest` | End-to-end over `WebApplicationFactory` + `TestAuthHandler` (reads `X-Test-UserId`) + FakeItEasy repos |

### What goes where

- **Per-format math** (bracket size, placeholder strings, tiebreaker cascade, position formulas) → `Domain.UnitTest` generator/ranker tests
- **Orchestration logic** (phase completion, schedule generation, bracket advancement propagation, volunteer shift validation) → `Application.UnitTest` service tests (e.g. `PhaseCompletionServiceTests`, `ScheduleGenerationServiceTests`)
- **HTTP contracts, auth tiers, end-to-end flows** → `Api.IntegrationTest`

### Anti-patterns

- Do **not** write integration tests for cases already covered by unit tests — pick the lowest layer that can express the behaviour.
- Do **not** assert on exact error-message substrings (`error.message.Contains("phase is completed")`) — assert on HTTP status + error code/field. Unit tests may check messages.
- When adding a repository interface method, update FakeItEasy mocks in integration tests.
- AutoMapper maps `null` `List<T>` to an empty list by default — use `.BeNullOrEmpty()` in assertions, not `.BeNull()`.

### Test modification policy (NEW features vs bug fixes)

- When adding a **new feature**, do **NOT** modify existing tests. Existing tests encode behaviour that must continue to hold.
- If an existing test fails as a result of new-feature work, **STOP and ask the user to review** — the failure may indicate an unintended regression rather than a needed update. Do not "fix" the test silently.
- This rule does **not** apply to bug fixes: if an existing test encodes the buggy behaviour, updating it is part of the fix. Call out the test change explicitly in the PR / hand-off so it can be reviewed.

---

## Known technical gotchas

- .NET 10 uses `.slnx` format, not `.sln`.
- Swashbuckle 10.x uses Microsoft.OpenApi v2 — types in `Microsoft.OpenApi` namespace (NOT `Microsoft.OpenApi.Models`). `OpenApiSecuritySchemeReference("id", document)` replaces the old pattern; `AddSecurityRequirement` takes `Func<OpenApiDocument, OpenApiSecurityRequirement>` — values must be `List<string>()` not `Array.Empty<string>()`.
- `Infrastructure.csproj` needs `<FrameworkReference Include="Microsoft.AspNetCore.App" />` AND the `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package.
- `AzureCosmosDisableNewtonsoftJsonCheck` must be set in `Directory.Build.props` (not just `Infrastructure.csproj`).
- Docker: nginx serves the SPA and proxies `/api/` to the backend (ports: API 5001→8080, SPA 3000→80).
- `create-vue` CLI is interactive — scaffold manually in non-interactive terminals.

---

## Naming conventions

- **Projects**: `KamSquare.KamScore.{Layer}`
- **Test projects**: `KamSquare.KamScore.{Layer}.{UnitTest|IntegrationTest}`
- **Config**: `appsettings.json` + `appsettings.Development.json`
- **Env vars**: Double-underscore for nested keys (e.g., `Jwt__Secret`)
- **Format files**: `XxxStrategy.cs` + `XxxGenerator.cs` + `XxxStandingsRanker.cs`
- **Format tests**: `XxxGeneratorTests.cs` + `XxxStandingsRankerTests.cs` (when ranker is non-trivial)
- **Requirements/design pairing**: identical basename across `docs/requirements/` and `docs/design/`
- **Application services**: `XxxService.cs` in `Application/Services/`, registered `AddScoped<XxxService>()` in `ServiceCollectionExtensions.cs`
- **Frontend shared dialogs**: domain-specific in `<domain>/Xxx{Form,Delete,Generate}Dialog.vue`; truly generic in `components/`
