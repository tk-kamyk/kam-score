# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## MANDATORY: Agent Team and Entry Point

All substantive work in this project runs through the **`agentic-dev-team` plugin** (source: https://github.com/bdfinst/agentic-dev-team).

- **Entry point is `/agentic-dev-team:orchestrator`.** If the user types bare `/orchestrator`, treat it as `/agentic-dev-team:orchestrator` — invoke the plugin orchestrator Skill immediately, do NOT search the repo or interpret the token as freeform text. If a request arrives in plain English without any slash command, still invoke the orchestrator first. Do not improvise a different agent lineup, do not do semantic reviews inline.
- **Every gate uses the team** — requirements drafting and review (Gate 1), BDD (Gate 2), failing tests (Gate 4), implementation (Gate 5), pre-handoff review (`/code-review --changed`), cleanup (Gate 7). Gate 1 is not exempt.
- **Model routing, three-phase workflow, progress files under `memory/`, and agent-capability matrix** all come from the plugin — read its docs rather than duplicating them here.
- **This project does NOT use `nwave-ai`.** Remove any `nwave-ai`-branded hooks or skills that appear in user- or project-level settings — they break agent dispatch.

---

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
- Build the React component with hardcoded/mock data
- No API calls yet — use static data matching the BDD scenarios
- Show the user for feedback
- Feature flags are **optional** (see "Feature Flags" below) — only gate behind a flag if the feature will be partially merged while still in development

### Gate 4: Failing Tests
- Write xUnit tests that express the BDD scenarios
- Domain unit tests for business logic
- Integration tests for API endpoints
- Create skeleton implementation classes (entities, DTOs, validators, repositories, endpoints) that throw `NotImplementedException` — the solution MUST compile
- ALL tests must FAIL at **runtime** (red), not at compile time, before implementation
- Run `dotnet test api/Continia.Card.slnx` to confirm failures
- **Self-review before handoff**: run `/agentic-dev-team:code-review --changed` against the Gate 4 changes and address findings before asking the user to review.

### Gate 5: API Implementation
- Implement domain logic, services, endpoints
- Run `dotnet test api/KamScore.slnx` — ALL tests must PASS (green)
- If any test fails, fix implementation (not the test) unless the test is wrong
- **Self-review before handoff**: run `/agentic-dev-team:code-review --changed` against every coding-gate output (Gate 4 tests and Gate 5 implementation). Address the findings BEFORE presenting the work for human approval. Only once the self-review is clean (or remaining findings are deliberately accepted with a stated reason) do you ask the user to review.

### Gate 6: Connect UI to API (skip if no frontend component)
- Replace mock data with real API calls
- Verify end-to-end flow works
- Run `cd spa && npm run lint && npx vue-tsc --noEmit`

### Gate 7: Cleanup
- If corrections were needed, add them to Code Standards below
- If requirements changed, verify `docs/requirements/` and `docs/design/` are both updated and still consistent
- If a new file exceeded 300 lines of meaningful code, consider whether to split (see size guidelines)

### Gate 8: Feature Flag Removal (optional — only if you added one in Gate 3)
- Remove the flag from appsettings and drop the `isEnabled` / `v-if` guard in the frontend
- The underlying feature-flag *mechanism* (see "Feature Flags") stays as boilerplate

---

## Documentation Standards

The project separates documentation into three intent-scoped directories, forming a progressive-detail ladder. The table below is the top-level map; detailed authoring rules for each directory live in its own `_index.md`.

| Directory | Intent | Writing style | Audience | Authoring rules |
|---|---|---|---|---|
| `docs/requirements/` | WHAT & WHY | Plain-language rule list | Product, delivery, auditors, non-developer IT | [_index.md](docs/requirements/_index.md) |
| `docs/bdd/` | HOW users experience it | Representative scenarios, not exhaustive | QA, delivery, stakeholders | [_index.md](docs/bdd/_index.md) |
| `docs/design/` | HOW the system is shaped | Brief intro + requirement-mirrored response | Engineering, reviewers | [_index.md](docs/design/_index.md) |

**Placement heuristic** — when a sentence could plausibly live in more than one place:

> If a sentence explains **what the system must do or satisfy** → requirements.
> If a sentence explains **how the system is shaped** or **why this shape** → design, under the requirement it serves.
> If a sentence describes **what a user observes** → BDD.

**Writing order** — requirement changes land in `docs/requirements/` first, then BDD, then design. Edge cases go to design, not BDD. Every BDD scenario and every design-side entry is tagged with its governing requirement ID.

---

## Project Overview

KamScore is a tournament management system for sports tournaments (Volleyball, Beach Volleyball) with flexible structures, scheduling, and live result entry. Multi-phase tournaments with round-robin and play-off formats.

Requirements: `docs/requirements/*.md` | Design details: `docs/design/*.md` | BDD specs: `docs/bdd/*.feature`

---

## Requirements & Design Docs

Requirements are split into focused per-area files. Implementation-level detail (formulas, exact game sequences, algorithm specifics) lives in an optional **paired design doc** with the **identical basename** under `docs/design/`.

| Area | Requirements | Paired design |
|------|--------------|---------------|
| Structure (phases, groups, overview) | `docs/requirements/structure.md` | — |
| Game generation + scheduling + referee | `docs/requirements/game-generation.md` | `docs/design/game-generation.md` |
| Results, standings, bracket advancement | `docs/requirements/results-and-standings.md` | `docs/design/results-and-standings.md` |
| Phase status, progression, placeholders | `docs/requirements/phase-advancement.md` | `docs/design/phase-advancement.md` |
| Restriction matrix by phase state | `docs/requirements/phase-state-restrictions.md` | — |
| Levels (per-phase divisions) | `docs/requirements/levels.md` | `docs/design/levels.md` |
| Tournament, team, court, user, volunteer, feature-flags | `docs/requirements/<area>.md` | as needed |

**Rule**: a requirement file states *what* the user can do. A design file states *how* we do it. Formulas, named constants (`bracketSize / 2^round + 1`), exact bracket walk orders, and step-by-step algorithms must not appear in the requirements — move them to the paired design doc and link with a `> See design: ./design/<name>.md` line at the top of the requirements file.

---

## Repository Structure

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
    components/   # Cross-domain shared UI (ConfirmDialog, ConfirmDeleteDialog, SectionHeader, CollapsiblePhaseCard, ...)
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
| Format-specific generation/standings | Strategy + static Generator/Ranker (Domain) | `RoundRobinGenerator`, `PlayoffEliminationStandingsRanker` |
| Cross-entity calculations | Domain services (static classes) | `GameScheduler`, `RefereeAssigner`, `PhaseAdvancementCalculator`, `BracketUtilities` |
| Multi-repository orchestration | Application services | `PhaseCompletionService`, `ScheduleGenerationService`, `BracketAdvancementService`, `VolunteerService`, `TournamentCopyService` |
| Request validation | FluentValidation validators (Application) | `GameResultDtoValidator`, `TournamentDtoValidator` |
| HTTP/auth concerns | API helpers or endpoint handlers | `TournamentAuthorizationHelper` |
| Simple CRUD wiring | Endpoint handlers directly | `TournamentEndpoints.CreateTournament()` |

**Rule**: If logic only needs the entity's own state → entity. If it needs multiple repositories or orchestrates cross-entity state transitions → Application service. API layer = HTTP concerns + delegation only.

### Domain Services

Static classes in `Domain/Services/` — cross-entity logic, no external dependencies:

| Service | Responsibility |
|---------|---------------|
| `StandingsCalculator` | Thin facade delegating to format-specific `*StandingsRanker` via `PhaseFormatStrategy` |
| `GameScheduler` | Schedules games across courts/time slots with conflict avoidance |
| `PlaceholderTeamGenerator` | Creates placeholder teams for cross-phase progression |
| `PlaceholderResolver` | Resolves/unresolves placeholder teams after phase completion |
| `PhaseAdvancementCalculator` | Determines qualifying teams and seeding |
| `BracketUtilities` | Bracket math (next power of 2, bracket ordering, within-phase advancement resolution) |
| `RefereeAssigner` | Assigns referees based on team availability |
| `VolunteerShiftCalculator` | Computes shift groups/slots for a tournament |

### Application Services

Extract when endpoint handlers grow beyond simple CRUD.

| Service | Responsibility |
|---------|---------------|
| `PhaseCompletionService` | Phase complete/reopen/delete-games, placeholder creation/regeneration/resolution |
| `ScheduleGenerationService` | Game generation + scheduling orchestration, phase activation |
| `BracketAdvancementService` | Propagates bracket placeholder resolution after a playoff result is recorded |
| `VolunteerService` | Volunteer CRUD + shift calculation + assignment validation |
| `TournamentCopyService` | Copy tournament structure from an existing tournament |

### Phase Format Strategy Pattern

All format-specific logic lives in `Domain/Services/Formats/` and follows a **uniform three-file layout** per format `Xxx`:

| File | Role |
|------|------|
| `XxxStrategy.cs` | Implements `IPhaseFormatStrategy`; thin orchestrator that delegates to the Generator and StandingsRanker |
| `XxxGenerator.cs` | Static class; owns `Generate()` — bracket/pairing construction, including any team-count validation |
| `XxxStandingsRanker.cs` | Static class; owns `Calculate()` (per-group standings) and `RankCrossGroup()` |

Formats: `RoundRobin`, `PlayoffElimination`, `PlayoffWithPlacement`, `DoubleElimination`, `DoubleEliminationVd`.

```
IPhaseFormatStrategy
├── GenerateGames()                 → delegates to XxxGenerator.Generate(...)
├── CalculateStandings()            → delegates to XxxStandingsRanker.Calculate(...)
├── RankCrossGroup()                → delegates to XxxStandingsRanker.RankCrossGroup(...)
├── SupportsRefereeAssignment       (property)
└── ValidateTeams()                 (format-specific team count validation, e.g. VD requires 8)
```

Factory: `PhaseFormatStrategy.For(PhaseFormat)` returns the correct instance.

**Adding a new format**:
1. New `PhaseFormat` enum value
2. New `XxxStrategy.cs` + `XxxGenerator.cs` + `XxxStandingsRanker.cs` triad
3. New case in `PhaseFormatStrategy.For()`
4. Test files: `XxxGeneratorTests.cs` (+ a ranker test file if the ranking is non-trivial) in `Domain.UnitTest`

No changes to consumers.

### Error Handling

`ExceptionHandlingMiddleware` converts domain exceptions to RFC 7807 Problem Details:

| Exception | HTTP Status |
|-----------|-------------|
| `NotFoundException` | 404 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ValidationException` | 400 |
| `PhaseStateException` | 409 |
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
| Phases | `/api/tournaments/{id}/structure/phases` | GET public, mutations JWT |
| Games | `/api/tournaments/{id}/games` | GET public, mutations JWT (result recording also accepts tournament code) |
| Standings / Final | `/api/tournaments/{id}/standings`, `/final-standings` | Public |
| Volunteers + shifts | `/api/tournaments/{id}/volunteers/**` | All endpoints JWT + owner/admin |
| Feature Flags | `GET /api/feature-flags` | Public (boilerplate — see "Feature Flags") |
| Health | `GET /api/health` | Public |

### Security Details
- JWT Bearer with HMAC-SHA256, config in `Jwt` section (Secret, Issuer, Audience, ExpirationMinutes)
- CORS via `Cors:AllowedOrigins`
- Owner verification: `tournament.IsOwnedBy(userId)` on all mutations
- Contact info (email, phone) on teams hidden from non-owners

---

## Feature Flags

The feature-flag *mechanism* is retained as intentional boilerplate — today there are **no active flags**. Keep the plumbing in place so a new in-development feature can be gated behind a flag without re-introducing the infrastructure each time.

- Backend endpoint: `GET /api/feature-flags` (unauthenticated)
- Frontend composable: `useFeatureFlags()` with `isEnabled(flag)` and session-scoped cache
- Requirements doc: `docs/requirements/feature-flags.md`

**Don't remove** the endpoint/composable/tests — they are marked as intentional boilerplate in their file headers.

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
- **DDD** — logic lives in domain entities and domain services
- **Options Pattern** for all configuration (`JwtOptions`, `UserOptions`, `CosmosDbOptions`, `CorsOptions`)
- **4-space indentation**, Allman brace style, `var` for type declarations
- **Record types** for DTOs, value objects, and simple data structures

### API standards

| Rule | Detail |
|------|--------|
| Minimal API | Static classes with `Map*Endpoints()` extension methods |
| Single DTO per entity | Nullable fields for response-only data (Id, OwnerId) |
| No service layer for pure CRUD | Endpoint handlers call repository directly |
| AutoMapper | Entity↔DTO mapping via assembly scanning |
| FakeItEasy | For all mocking (not Moq, not InMemory equivalents) |
| FluentValidation | Auto-discovered from Application assembly |
| Public setters on entities | For Cosmos DB serialization; logic enforced via `Create`/`Update` methods |
| `[GeneratedRegex]` | With `partial class` when entities need regex validation |

### Domain logic placement
- Single-entity rules → entity class (e.g., `Tournament.IsCodeValid()`)
- Format-specific logic → `Xxx{Strategy,Generator,StandingsRanker}` triad (see Phase Format Strategy Pattern)
- Other cross-entity domain logic → Domain services (static classes in `Domain/Services/`)
- Multi-repository orchestration, cross-entity state transitions → Application services

### ANTI-PATTERNS — do NOT put these in endpoint handlers

The endpoint is a thin controller. Push logic down if you see any of these:

- **Field classification** (e.g., deciding which DTO fields are "structural")
- **Conditional business logic** (e.g., "if format changed, block update")
- **Cross-entity coordination** (e.g., fetching games + structure + teams to compute a transition)
- **Cascading state changes** (e.g., `if (phase.Status is Scheduled or InProgress) structure.ResetPhase(...)` — that belongs in an Application service)
- **Multi-repo writes that must succeed together** (wrap them in a service method so the intent is named)
- **Old/new state comparisons** for business decisions
- Business rule checks beyond simple null/empty guards

Endpoints should ONLY: validate auth, map DTOs, call validator, delegate to domain/service, map response, return HTTP.

**Canonical examples of well-named service methods** (extracted because the endpoint was doing too much):
- `PhaseCompletionService.DeletePhaseGamesAsync` — owns "games deleted → phase resets to New"
- `BracketAdvancementService.ResolveAsync` — owns "playoff result recorded → downstream placeholders resolve"
- `VolunteerService.AssignToShiftAsync` — owns "validate shift time is a real slot, assign, persist"

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
- Extract when ANY of: multi-repo coordination, conditional cross-entity logic, cascading state changes, reuse across endpoints
- Keep services focused — one cohesive responsibility per service
- Register as `AddScoped` in `ServiceCollectionExtensions.cs`

### File size guidelines
- Domain strategy files: prefer the three-file triad over a monolith — keep each file under ~400 lines
- Endpoint files: under ~200 lines; push logic into Application services otherwise
- Vue single-file components: under ~250 lines; extract sub-components (dialogs, list rows, filter bars) otherwise

---

## Testing

### Test layout

| Project | Scope |
|---------|-------|
| `Domain.UnitTest` | Entities, Domain services (including `XxxGeneratorTests`, `XxxStandingsRankerTests`), value objects. Pure, no mocks |
| `Application.UnitTest` | Validators, mappers, Application services. Repositories are FakeItEasy fakes |
| `Api.IntegrationTest` | End-to-end over `WebApplicationFactory` + `TestAuthHandler` (reads `X-Test-UserId`) + FakeItEasy repos |

### What goes where
- **Per-format math** (bracket size, placeholder strings, tiebreaker cascade, position formulas) → `Domain.UnitTest` generator/ranker tests
- **Orchestration logic** (phase completion, schedule generation, bracket advancement propagation, volunteer shift validation) → `Application.UnitTest` service tests (e.g. `PhaseCompletionServiceTests`, `ScheduleGenerationServiceTests`)
- **HTTP contracts, auth tiers, end-to-end flows** → `Api.IntegrationTest`

### Anti-patterns
- Do **not** write integration tests for cases that are already covered by unit tests — pick the lowest layer that can express the behaviour
- Do **not** assert on exact error-message substrings (`error.message.Contains("phase is completed")`) — assert on HTTP status + error code/field. Unit tests may check messages
- When adding a repository interface method, update FakeItEasy mocks in integration tests
- AutoMapper maps `null` `List<T>` to an empty list by default — use `.BeNullOrEmpty()` in assertions, not `.BeNull()`

---

## Frontend Standards

### Structure
- **Vue 3** Composition API, single file components
- **Group by domain** (`auth/`, `tournament/`, `team/`, `court/`, `game/`, `structure/`, `standings/`, `volunteer/`), not by layer
- Shared infra (`api/client.ts`, `composables/`, `router/`) separate from domain folders
- Cross-domain shared UI components live in `components/` (e.g. `ConfirmDialog`, `ConfirmDeleteDialog`, `SectionHeader`, `CollapsiblePhaseCard`)
- `views/` = thin route wrappers only

### Component size & extraction
- Keep components **under ~250 lines** (template + script)
- When a list page owns multiple dialogs, extract each dialog into its own `Xxx{Form,Generate,Delete}Dialog.vue` in the same domain folder
- Use the shared `ConfirmDialog.vue` / `ConfirmDeleteDialog.vue` for simple confirmations; only hand-roll a dialog when it has a form or non-trivial body
- Dialogs own their own form state and validation; the parent only owns `showXDialog` booleans and the save/delete handlers

### Reactivity
- **`computed` over inline expressions**: use `computed()` for any derived state — keeps templates clean and caches results. Do not duplicate reactive expressions in the template or recalculate in methods

### Styling
- **Responsive sizing**: Vuetify 4 responsive utility classes over custom CSS media queries
- **Responsive layout**: Vuetify flex utilities (`d-flex`, `flex-sm-row`, `flex-wrap`, `ga-4`)
- **CSS Cascade Layers**: custom CSS targeting Vuetify internals must use `@layer overrides { ... }`
- Layer order declaration in `index.html` inline `<style>`

### Linting & formatting
ESLint (flat config in `spa/eslint.config.js`) + Prettier (`spa/.prettierrc.json`) enforce style. Any generated or edited frontend code MUST conform:
- Prettier: single quotes, NO semicolons, trailing commas (all), 100-char print width, `arrowParens: always`
- ESLint: `@eslint/js` recommended + `typescript-eslint` recommended + `eslint-plugin-vue` flat/recommended, with Prettier compatibility via `@vue/eslint-config-prettier/skip-formatting`
- No `any` — use specific types or generics; prefix intentionally unused vars/args/destructures with `_`
- `npm run lint` auto-fixes ESLint issues; `npm run format` reformats with Prettier; `npm run build` runs format + lint + type-check + vite build (also executed by `spa/Dockerfile`)

---

## Known Technical Gotchas

- .NET 10 uses `.slnx` format, not `.sln`
- Swashbuckle 10.x uses Microsoft.OpenApi v2 — types in `Microsoft.OpenApi` namespace (NOT `Microsoft.OpenApi.Models`). `OpenApiSecuritySchemeReference("id", document)` replaces the old pattern; `AddSecurityRequirement` takes `Func<OpenApiDocument, OpenApiSecurityRequirement>` — values must be `List<string>()` not `Array.Empty<string>()`
- `Infrastructure.csproj` needs `<FrameworkReference Include="Microsoft.AspNetCore.App" />` AND the `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package
- `AzureCosmosDisableNewtonsoftJsonCheck` must be set in `Directory.Build.props` (not just `Infrastructure.csproj`)
- Docker: nginx serves SPA and proxies `/api/` to backend (ports: API 5001→8080, SPA 3000→80)
- `create-vue` CLI is interactive — scaffold manually in non-interactive terminals

---

## Naming Conventions

- **Projects**: `KamSquare.KamScore.{Layer}`
- **Test projects**: `KamSquare.KamScore.{Layer}.{UnitTest|IntegrationTest}`
- **Config**: `appsettings.json` + `appsettings.Development.json`
- **Env vars**: Double-underscore for nested keys (e.g., `Jwt__Secret`)
- **Format files**: `XxxStrategy.cs` + `XxxGenerator.cs` + `XxxStandingsRanker.cs`
- **Format tests**: `XxxGeneratorTests.cs` + `XxxStandingsRankerTests.cs` (when ranker is non-trivial)
- **Requirements/design pairing**: identical basename across `docs/requirements/` and `docs/design/`
- **Application services**: `XxxService.cs` in `Application/Services/`, registered `AddScoped<XxxService>()` in `ServiceCollectionExtensions.cs`
- **Frontend shared dialogs**: domain-specific in `<domain>/Xxx{Form,Delete,Generate}Dialog.vue`; truly generic in `components/`
