# Testing & Running the Application

## Running the Application

### Option 1: Docker Compose (full stack)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| SPA | http://localhost:3000 |
| API | http://localhost:5001 |
| Swagger | http://localhost:5001/swagger |

The SPA's nginx proxies `/api/` requests to the API container. Cosmos DB connection string is read from the `.env` file (`COSMOSDB_CONNECTION_STRING`).

Test credentials: `admin` / `admin123`

### Option 2: Local development (no Docker)

Start the API and SPA separately:

```bash
# Terminal 1 — API (port 5250)
dotnet run --project api/src/KamSquare.KamScore.Api

# Terminal 2 — SPA dev server (port 5173)
cd spa && npm run dev
```

| Service | URL |
|---------|-----|
| SPA (Vite dev server) | http://localhost:5173 |
| API | http://localhost:5250 |
| Swagger | http://localhost:5250/swagger |

The Vite dev server proxies `/api/` requests to `http://localhost:5001`. To use local `dotnet run` (port 5250) instead of Docker, update the proxy target in `spa/vite.config.ts`:

```ts
proxy: {
  '/api': {
    target: 'http://localhost:5250',  // match dotnet run port
    changeOrigin: true
  }
}
```

The API reads Cosmos DB credentials from user secrets in Development mode:

```bash
cd api/src/KamSquare.KamScore.Api
dotnet user-secrets init
dotnet user-secrets set "CosmosDb:ConnectionString" "<your-connection-string>"
```

Test credentials are configured in `appsettings.Development.json`: `admin` / `admin123`

## API Endpoints

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/api/health` | Public |
| `POST` | `/api/auth/login` | Public |
| `GET` | `/api/tournaments` | Public (anonymous: all without codes; authenticated: own with codes) |
| `GET` | `/api/tournaments/{id}` | Public (owner sees tournament code, anonymous doesn't) |
| `POST` | `/api/tournaments` | JWT required |
| `PUT` | `/api/tournaments/{id}` | JWT required + ownership |
| `DELETE` | `/api/tournaments/{id}` | JWT required + ownership |

## Tests

**36 tests total** across 3 test projects.

Run all tests:

```bash
dotnet test api/KamScore.slnx
```

### Domain Unit Tests (10 tests)

`api/tests/KamSquare.KamScore.Domain.UnitTest/TournamentTests.cs`

| Test | Description |
|------|-------------|
| `Constructor_ShouldSetProperties` | Properties match constructor arguments |
| `Constructor_ShouldGenerateId` | Id is a non-empty GUID string |
| `Constructor_ShouldGenerateTournamentCode` | Code is auto-generated on creation |
| `GenerateTournamentCode_ShouldProduceValidFormat` | Code matches `[A-Z]{4}[0-9]` pattern |
| `IsOwnedBy_ShouldReturnTrue_ForOwner` | Owner check returns true for correct user |
| `IsOwnedBy_ShouldReturnFalse_ForOtherUser` | Owner check returns false for different user |
| `Update_ShouldChangeName` | Name updated via Update method |
| `Update_ShouldChangeDiscipline` | Discipline updated via Update method |
| `Update_ShouldSetGameConditions` | GameConditions updated via Update method |
| `Constructor_ShouldInitializeEmptyCollections` | Teams, Courts, Phases start as empty lists |

### Application Unit Tests (11 tests)

**Validators** — `api/tests/KamSquare.KamScore.Application.UnitTest/Validators/TournamentDtoValidatorTests.cs`

| Test | Description |
|------|-------------|
| `Valid_Dto_ShouldPassValidation` | Valid input passes all rules |
| `EmptyName_ShouldFailValidation` | Name is required |
| `InvalidDiscipline_ShouldFailValidation` | Must be a known discipline |
| `NegativeGameLength_ShouldFailValidation` | GameLength must be positive |
| `InvalidBestOfSets_ShouldFailValidation` | BestOfSets must be 1, 3, or 5 |
| `ValidBestOfSets_ShouldPassValidation` | Valid BestOfSets values (1, 3, 5) pass |
| `MismatchedPointsPerSetCount_ShouldFailValidation` | PointsPerSet count must match BestOfSets |
| `ValidGameConditions_ShouldPassValidation` | Valid GameConditions pass |
| `BeachVolleyball_ShouldBeValidDiscipline` | BeachVolleyball is accepted |

**Mappers** — `api/tests/KamSquare.KamScore.Application.UnitTest/Mappers/TournamentProfileTests.cs`

| Test | Description |
|------|-------------|
| `Configuration_ShouldBeValid` | AutoMapper profile configuration is valid |
| `Tournament_To_TournamentDto_ShouldMapCorrectly` | Entity maps to DTO with correct field values |
| `Tournament_WithGameConditions_ShouldMapCorrectly` | GameConditions maps correctly |

### Integration Tests (15 tests)

All use `WebApplicationFactory` with FakeItEasy mocks and `TestAuthHandler`.

**Auth** — `api/tests/KamSquare.KamScore.Api.IntegrationTest/AuthApiTests.cs`

| Test | Description |
|------|-------------|
| `Login_ValidCredentials_ShouldReturnToken` | Valid login returns JWT token |
| `Login_InvalidCredentials_ShouldReturn401` | Bad credentials return 401 |

**Tournaments** — `api/tests/KamSquare.KamScore.Api.IntegrationTest/TournamentApiTests.cs`

| Test | Description |
|------|-------------|
| `CreateTournament_Authenticated_ShouldReturnCreated` | Authenticated user can create |
| `CreateTournament_Anonymous_ShouldReturn401` | Anonymous create returns 401 |
| `GetTournaments_Anonymous_ShouldReturnAllWithoutCodes` | Anonymous gets all tournaments, codes hidden |
| `GetTournaments_Authenticated_ShouldReturnOwnWithCodes` | Authenticated gets own tournaments with codes |
| `GetTournament_Owner_ShouldIncludeCode` | Owner sees tournament code |
| `GetTournament_Anonymous_ShouldExcludeCode` | Anonymous doesn't see code |
| `UpdateTournament_Owner_ShouldSucceed` | Owner can update own tournament |
| `UpdateTournament_NonOwner_ShouldReturn403` | Non-owner gets 403 |
| `DeleteTournament_Owner_ShouldSucceed` | Owner can delete own tournament |
| `DeleteTournament_NonOwner_ShouldReturn403` | Non-owner gets 403 |
| `DeleteTournament_Anonymous_ShouldReturn401` | Anonymous delete returns 401 |
| `CreateTournament_WithGameConditions_ShouldStoreCorrectly` | GameConditions persisted correctly |

**Health** — `api/tests/KamSquare.KamScore.Api.IntegrationTest/HealthApiTests.cs`

| Test | Description |
|------|-------------|
| `Health_ShouldReturnOk` | Health endpoint returns 200 |
