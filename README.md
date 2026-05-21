# KamScore

Tournament management system for sports tournaments (Volleyball, Beach Volleyball) with
flexible structures, scheduling, and live result entry. Supports multi-phase tournaments
with round-robin and play-off formats.

- Backend: .NET 10 minimal API
- Frontend: Vue 3 + TypeScript + Vuetify
- Storage: Azure Cosmos DB
- Hosting: Azure App Service / Static Web App (primary), Azure Container Apps (secondary)

See [`docs/requirements/`](docs/requirements/) for the feature specification and
[`docs/design/`](docs/design/) for implementation details.

## Running the application

### Docker Compose (full stack)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| SPA | http://localhost:3000 |
| API | http://localhost:5001 |
| Swagger | http://localhost:5001/swagger |

The SPA's nginx proxies `/api/` requests to the API container. Cosmos DB connection string is read from `.env` (`COSMOSDB_CONNECTION_STRING`). Test credentials: `admin` / `admin123`.

### Local development (no Docker)

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

The Vite dev server proxies `/api/` to `http://localhost:5001`. To target a local `dotnet run` (port 5250) instead, change the proxy target in `spa/vite.config.ts`.

The API reads Cosmos DB credentials from user secrets in Development mode:

```bash
cd api/src/KamSquare.KamScore.Api
dotnet user-secrets init
dotnet user-secrets set "CosmosDb:ConnectionString" "<your-connection-string>"
```

Test credentials are configured in `appsettings.Development.json`: `admin` / `admin123`.

## Tests

```bash
dotnet test api/KamScore.slnx                                  # Backend unit + integration
cd spa && npm run build                                        # Frontend lint + typecheck + build
```

## Licence

Source-available, all rights reserved. See [LICENSE](./LICENSE). Any use beyond
on-platform viewing/forking requires explicit written permission from the
copyright holder.

## Security

To report a vulnerability privately, see [SECURITY.md](./SECURITY.md).
