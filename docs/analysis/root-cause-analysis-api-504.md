# Root Cause Analysis: API 504 Gateway Timeout

**Date:** 2026-02-25
**Symptom:** SPA loads successfully, but every API call results in a long wait followed by a 504 Gateway Timeout.
**Scope:** Azure Container Apps deployment; SPA -> nginx -> API container networking and API container startup.

---

## Executive Summary

Three independent root causes were identified that each contribute to the 504 Gateway Timeout. The primary cause (RC-A) is that the nginx reverse proxy inside the SPA container cannot resolve the API container hostname because Azure Container Apps does not use Docker-style service names for internal DNS. Two additional causes (RC-B, RC-C) would produce further failures even if networking were fixed: the API blocks on Cosmos DB initialization at startup with no timeout, and CORS is not configured for the production domain.

---

## Evidence Inventory

| ID | Source File | Key Finding |
|----|------------|-------------|
| E1 | `infra/main.bicep:137-139` | SPA env var `API_BACKEND_URL` set to `https://REDACTED-API` |
| E2 | `spa/nginx.conf:7` | nginx proxies `/api/` to `${API_BACKEND_URL}/api/` |
| E3 | `spa/Dockerfile:11` | Default `API_BACKEND_URL=http://api:8080` (Docker Compose name) |
| E4 | `infra/main.bicep:169-171` | API ingress: `external: false`, `targetPort: 8080` |
| E5 | `api/src/.../Program.cs:67` | `await app.Services.InitializeCosmosDbAsync()` blocks before `app.Run()` |
| E6 | `api/src/.../ServiceCollectionExtensions.cs:84-92` | `InitializeCosmosDbAsync` calls `CreateDatabaseIfNotExistsAsync` with no timeout/cancellation |
| E7 | `api/src/.../ServiceCollectionExtensions.cs:35-50` | Cosmos client only registered if connection string is non-empty; no fallback or health warning |
| E8 | `api/src/.../appsettings.json:13-14` | Default CORS `AllowedOrigins: []` -- no production origins configured |
| E9 | `infra/main.bicep:216-239` | API env vars: no `Cors__AllowedOrigins__0` configured |
| E10 | `infra/main.bicep:143-148` | SPA scale: `minReplicas: 0`, `maxReplicas: 2` |
| E11 | `api/src/.../ServiceCollectionExtensions.cs:24-29` | `JwtOptions` validated on start -- empty secret fails startup |
| E12 | `spa/src/api/client.ts:4` | SPA uses `VITE_API_BASE_URL` or defaults to `/api` (relative) |
| E13 | `infra/main.bicep:103-152` | SPA Container App has external ingress on port 80 |
| E14 | `spa/nginx.conf:8` | `proxy_set_header Host $proxy_host` -- forwards proxy target host, not original |

---

## 5 Whys Analysis

### Branch A: Nginx Cannot Reach API Container (PRIMARY)

**WHY 1 -- What is the immediate failure?**
The SPA's nginx proxy receives requests on `/api/` and attempts to forward them to the API backend, but the upstream connection times out, producing a 504.

*Evidence:* `spa/nginx.conf:6-12` proxies to `${API_BACKEND_URL}/api/`. The Bicep template sets `API_BACKEND_URL=https://REDACTED-API` (E1).

**WHY 2 -- Why does the upstream connection time out?**
The hostname `REDACTED-API` does not resolve correctly from within the SPA container. In Azure Container Apps, internal apps within the same environment are reachable via `<app-name>.internal.<env-default-domain>` or the full internal FQDN, NOT the bare container app name. The bare name `REDACTED-API` is not a valid DNS entry.

*Evidence:* Azure Container Apps internal ingress documentation states that apps must be addressed by their internal FQDN (`<app-name>.internal.<unique-id>.<region>.azurecontainerapps.io`). The Bicep template uses the bare name `https://REDACTED-API` (E1, line 138), which has no DNS record.

**WHY 3 -- Why was the bare container app name used instead of the internal FQDN?**
The Bicep template hardcodes `API_BACKEND_URL` to `https://REDACTED-API` rather than deriving it from the API container app's actual internal FQDN. This pattern was likely carried over from the `docker-compose.yml` where Docker DNS resolves bare service names (E3: `http://api:8080`).

*Evidence:* `infra/main.bicep:138` hardcodes `value: 'https://REDACTED-API'` instead of using the Bicep expression `'https://${apiApp.properties.configuration.ingress.fqdn}'` (which is already used in the outputs at line 259).

**WHY 4 -- Why was there no validation that the SPA can reach the API?**
There is no health check or readiness probe configured on the SPA container that would verify API connectivity. The SPA container app has no probes defined in Bicep (E13). Additionally, there is no integration test or smoke test in the CD pipeline that validates end-to-end connectivity after deployment.

*Evidence:* `infra/main.bicep:103-152` -- SPA container definition has no `probes` property. `.github/workflows/cd.yml:103-112` -- the deploy step runs Bicep deployment only, with no post-deployment validation.

**WHY 5 -- Root Cause**
The infrastructure-as-code was designed without accounting for Azure Container Apps' internal DNS model. The assumption that bare container app names resolve (as they do in Docker Compose) was never tested in a production-like environment, and no post-deployment smoke test exists to catch networking failures.

---

### Branch B: API Startup Blocks Indefinitely on Cosmos DB Initialization

**WHY 1 -- What happens if the API container does start but Cosmos DB is slow or unreachable?**
The API application hangs at startup, never begins listening on port 8080, and the ingress health check (or nginx proxy) times out.

*Evidence:* `Program.cs:67` -- `await app.Services.InitializeCosmosDbAsync()` is called AFTER `app.Build()` but BEFORE `app.Run()`. This is a blocking call in the startup path.

**WHY 2 -- Why does `InitializeCosmosDbAsync` block indefinitely?**
The method calls `cosmosClient.CreateDatabaseIfNotExistsAsync()` and `database.Database.CreateContainerIfNotExistsAsync()` with no `CancellationToken`, no timeout, and no try/catch. If the Cosmos DB endpoint is unreachable, slow, or the connection string is invalid, the SDK's default retry policy will retry for an extended period (potentially minutes).

*Evidence:* `ServiceCollectionExtensions.cs:84-92` -- no timeout, no cancellation token, no exception handling. The `CosmosClient` default retry policy includes exponential backoff with multiple retries.

**WHY 3 -- Why is there no startup timeout or circuit breaker?**
The initialization was written as a simple fire-and-forget-success pattern without considering infrastructure failure modes. There is no host startup timeout configured, no `IHostApplicationLifetime` integration, and no fallback behavior.

*Evidence:* No `WebApplicationBuilder` host timeout configuration in `Program.cs`. No try/catch in `InitializeCosmosDbAsync`.

**WHY 4 -- Why would Cosmos DB be unreachable in production?**
The Cosmos DB connection string is fetched at deployment time from the Cosmos account via `listConnectionStrings()` and stored as a Container App secret (E9, line 203). This should work IF the Cosmos account exists and the managed identity has access. However, network restrictions on the Cosmos account (IP firewalls, VNET rules) could block connections from the Container App Environment's outbound IPs. This was not verified.

*Evidence:* `infra/main.bicep:39` fetches the connection string. No network rule configuration is visible for the Cosmos account (it is an `existing` resource in a shared resource group). No Container App Environment VNET integration is configured.

**WHY 5 -- Root Cause**
The API startup path includes a synchronous blocking dependency on an external service (Cosmos DB) with no timeout, no error handling, and no health check separation. Combined with no network connectivity validation between the Container App Environment and the Cosmos DB account, this creates a silent hang on startup.

---

### Branch C: CORS Not Configured for Production Domain

**WHY 1 -- What happens when the SPA makes a direct API call?**
Even if the nginx proxy path (Branch A) is fixed so that API calls are proxied, any direct browser-to-API calls (e.g., if the architecture changes or during debugging) would be blocked by CORS.

*Evidence:* `appsettings.json:17` -- `AllowedOrigins: []`. `appsettings.Development.json:3` -- only `http://localhost:5173` is configured. No production CORS origin is set.

**WHY 2 -- Why is no production CORS origin configured?**
The API container app's environment variables in Bicep (E9) do not include a `Cors__AllowedOrigins__0` entry. The production `appsettings.json` defaults to an empty array.

*Evidence:* `infra/main.bicep:216-239` -- env vars list includes JWT, Users, CosmosDb, and ASPNETCORE_ENVIRONMENT, but no CORS configuration.

**WHY 3 -- Why was CORS omitted from the infrastructure configuration?**
The current architecture uses nginx as a reverse proxy, so the developer may have assumed CORS is unnecessary (same-origin from the browser's perspective). However, this assumption is fragile: it breaks if the API is ever called directly, and it means error responses from the API during proxy failures cannot be properly handled.

**WHY 4 -- Root Cause**
Missing CORS configuration in the production IaC template. While not the primary cause of the 504, it represents an incomplete production configuration that would surface as a separate failure class if the proxy architecture changes.

---

### Branch D: SPA Container Scales to Zero

**WHY 1 -- Could the SPA itself be unavailable intermittently?**
The SPA container app is configured with `minReplicas: 0` (E10), meaning it can scale to zero when idle. This would cause cold-start latency on the first request, but should not cause a 504 for API calls since the 504 occurs AFTER the SPA loads.

*Evidence:* `infra/main.bicep:144` -- `minReplicas: 0`. However, the problem statement says "SPA loads fine" so at least one replica is running when the user observes the 504.

**Assessment:** Contributing factor to user experience (cold starts), but NOT a root cause of the 504 on API calls. Noted for completeness.

---

## Backwards Chain Validation

### Chain A Validation (Nginx DNS Resolution)
IF `API_BACKEND_URL` is set to `https://REDACTED-API` (a non-resolvable hostname in Azure Container Apps)
THEN nginx will attempt to resolve `REDACTED-API` via DNS
THEN DNS resolution fails or returns no address
THEN nginx's `proxy_pass` has no upstream to connect to
THEN the client receives a 504 Gateway Timeout after nginx's proxy timeout expires.
**VALIDATED: This chain fully explains the observed symptom.**

### Chain B Validation (Cosmos DB Blocking Startup)
IF Cosmos DB is unreachable from the Container App Environment (network rules, wrong endpoint)
THEN `CreateDatabaseIfNotExistsAsync` retries with exponential backoff
THEN `app.Run()` is never reached
THEN the API container never starts listening on port 8080
THEN Azure Container Apps ingress gets no healthy backend
THEN even if DNS resolved correctly, the API would return 502/504.
**VALIDATED: This chain independently produces the observed symptom.**

### Chain C Validation (CORS)
IF CORS `AllowedOrigins` is empty in production
THEN browser preflight OPTIONS requests to the API would be rejected
THEN the browser blocks the response (but the HTTP status would be 403/CORS error, not 504).
**VALIDATED as secondary issue: Produces a different symptom (CORS error) than the reported 504. Not the primary cause but would cause failures if proxy is bypassed.**

---

## Root Causes Summary

| ID | Root Cause | Severity | Confidence |
|----|-----------|----------|------------|
| RC-A | **Incorrect API_BACKEND_URL in Bicep**: `https://REDACTED-API` is not a valid internal hostname in Azure Container Apps. Must use the internal FQDN. | CRITICAL | HIGH -- this is almost certainly the primary cause of the 504 |
| RC-B | **API startup blocks indefinitely on Cosmos DB initialization**: No timeout, no cancellation, no error handling on `InitializeCosmosDbAsync`. If Cosmos is unreachable, API never starts. | HIGH | MEDIUM -- depends on Cosmos DB network accessibility |
| RC-C | **No CORS configuration for production**: `AllowedOrigins` is empty in production env vars. | MEDIUM | HIGH -- confirmed from Bicep template |

---

## Recommended Solutions

### Immediate Mitigations (Restore Service)

| Action | Addresses | Effort |
|--------|----------|--------|
| M1: Update `API_BACKEND_URL` in Bicep to use the API container app's internal FQDN: `'https://${apiApp.properties.configuration.ingress.fqdn}'` | RC-A | LOW |
| M2: Verify Cosmos DB network rules allow connections from Container App Environment outbound IPs | RC-B | LOW |
| M3: Add `Cors__AllowedOrigins__0` env var to API container app in Bicep, set to SPA's external FQDN | RC-C | LOW |

### Permanent Fixes (Prevent Recurrence)

| Action | Addresses | Effort |
|--------|----------|--------|
| P1: In `infra/main.bicep`, change SPA's `API_BACKEND_URL` from hardcoded `'https://REDACTED-API'` to `'https://${apiApp.properties.configuration.ingress.fqdn}'` | RC-A | LOW |
| P2: Add startup timeout and error handling to `InitializeCosmosDbAsync`: wrap in try/catch, add `CancellationToken` with timeout (e.g., 30 seconds), log failure, and allow app to start in degraded mode or fail fast with a clear error | RC-B | MEDIUM |
| P3: Add a startup health check endpoint that does NOT depend on Cosmos DB, and a readiness probe that does. Configure Container Apps liveness and readiness probes in Bicep. | RC-B | MEDIUM |
| P4: Add a post-deployment smoke test step in `cd.yml` that calls the API health endpoint and verifies a 200 response | RC-A, RC-B | MEDIUM |
| P5: Add CORS production origin as an env var in Bicep, derived from SPA's FQDN: `'https://${spaApp.properties.configuration.ingress.fqdn}'` | RC-C | LOW |
| P6: Set SPA `minReplicas: 1` to avoid cold-start latency | Enhancement | LOW |

### Early Detection Measures

| Measure | Detects |
|---------|---------|
| Container Apps liveness probe on `/api/health` | API container not responding |
| Container Apps readiness probe that tests Cosmos DB connectivity | Database unreachable |
| Azure Monitor alert on 5xx rate > 0 for the SPA container app | Proxy failures |
| Post-deploy smoke test in CI/CD | Networking/config issues at deploy time |

---

## Detailed Fix for RC-A (Primary Root Cause)

The critical fix is in `/Users/tomaszkaminski/Workspace/kam-score/infra/main.bicep`, lines 136-139.

**Current code (broken):**
```bicep
env: [
  {
    name: 'API_BACKEND_URL'
    value: 'https://REDACTED-API'
  }
]
```

**Required fix:**
```bicep
env: [
  {
    name: 'API_BACKEND_URL'
    value: 'https://${apiApp.properties.configuration.ingress.fqdn}'
  }
]
```

This requires that the `spaApp` resource has a dependency on `apiApp` so that the FQDN is available at deploy time. Currently `spaApp` is defined BEFORE `apiApp` in the Bicep file (lines 103-152 vs 158-253), so either:
1. Reorder the resources so `apiApp` is defined first, OR
2. Add an explicit `dependsOn: [apiApp]` to the `spaApp` resource (Bicep may infer this from the property reference, but explicit is safer)

**Note on protocol:** The current value uses `https://` which is correct for Azure Container Apps internal ingress (internal traffic uses TLS via the platform). The `fqdn` property already includes the full hostname without protocol, so `'https://${apiApp.properties.configuration.ingress.fqdn}'` is the correct format.

---

## Detailed Fix for RC-B (Cosmos DB Startup Blocking)

The fix is in `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, lines 84-92.

**Current code (blocks indefinitely):**
```csharp
public static async Task InitializeCosmosDbAsync(this IServiceProvider services)
{
    var cosmosClient = services.GetService<CosmosClient>();
    if (cosmosClient is null) return;

    var options = services.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
    var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(options.DatabaseName);
    await database.Database.CreateContainerIfNotExistsAsync(options.ContainerName, "/ownerId");
}
```

**Required fix:** Add timeout, cancellation, error handling, and logging:
```csharp
public static async Task InitializeCosmosDbAsync(this IServiceProvider services)
{
    var cosmosClient = services.GetService<CosmosClient>();
    if (cosmosClient is null) return;

    var logger = services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("CosmosDbInitialization");
    var options = services.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    try
    {
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            options.DatabaseName, cancellationToken: cts.Token);
        await database.Database.CreateContainerIfNotExistsAsync(
            options.ContainerName, "/ownerId", cancellationToken: cts.Token);
        logger.LogInformation("Cosmos DB initialized: {Database}/{Container}",
            options.DatabaseName, options.ContainerName);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to initialize Cosmos DB within 30 seconds. " +
            "The application will start but database operations will fail.");
        // Fail fast in production rather than running in a broken state:
        throw;
    }
}
```

---

## Investigation Notes

- **SPA client is correctly configured**: `spa/src/api/client.ts` defaults to `/api` (relative path), which means in production the browser sends requests to the SPA's own origin, and nginx proxies them. This is correct. The problem is nginx's inability to reach the backend, not the SPA's request path.

- **nginx `Host` header**: `spa/nginx.conf:8` sets `proxy_set_header Host $proxy_host` which forwards the proxy target's hostname. This is acceptable for Container Apps internal routing but could cause issues if the API validates the Host header. Low risk but worth noting.

- **API ingress is internal-only**: `infra/main.bicep:170` sets `external: false` on the API container app. This is correct -- the API should only be reachable from within the Container App Environment. But it means the FQDN will be an internal one (`*.internal.*`), which is what nginx needs.

- **Key Vault URI construction**: `infra/main.bicep:40` constructs `keyVaultUri` using `environment().suffixes.keyvaultDns`. This could produce a malformed URL if the suffix already includes a leading dot. Worth verifying but unlikely to cause the 504 since secrets would fail at deployment time (Bicep validation), not at runtime.

---

## Files Examined

| File | Path |
|------|------|
| CD Pipeline | `/Users/tomaszkaminski/Workspace/kam-score/.github/workflows/cd.yml` |
| Bicep Main | `/Users/tomaszkaminski/Workspace/kam-score/infra/main.bicep` |
| Bicep Module | `/Users/tomaszkaminski/Workspace/kam-score/infra/modules/shared-rg-access.bicep` |
| API Dockerfile | `/Users/tomaszkaminski/Workspace/kam-score/api/Dockerfile` |
| SPA Dockerfile | `/Users/tomaszkaminski/Workspace/kam-score/spa/Dockerfile` |
| nginx config | `/Users/tomaszkaminski/Workspace/kam-score/spa/nginx.conf` |
| docker-compose | `/Users/tomaszkaminski/Workspace/kam-score/docker-compose.yml` |
| Program.cs | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Api/Program.cs` |
| ServiceCollectionExtensions | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` |
| appsettings.json | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Api/appsettings.json` |
| appsettings.Development.json | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Api/appsettings.Development.json` |
| SPA API client | `/Users/tomaszkaminski/Workspace/kam-score/spa/src/api/client.ts` |
| Vite config | `/Users/tomaszkaminski/Workspace/kam-score/spa/vite.config.ts` |
| Health endpoints | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Api/Endpoints/HealthEndpoints.cs` |
| CosmosDbOptions | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Infrastructure/Options/CosmosDbOptions.cs` |
| CorsOptions | `/Users/tomaszkaminski/Workspace/kam-score/api/src/KamSquare.KamScore.Infrastructure/Options/CorsOptions.cs` |
| .env.example | `/Users/tomaszkaminski/Workspace/kam-score/.env.example` |
