# Feature Flags (mechanism)

**Status**: No active flags today. This plumbing is kept as intentional boilerplate so new features can be gated behind a flag during development without re-introducing the infrastructure each time.

## Mechanism

- Feature flags are configured in appsettings as flat boolean key-value pairs under the `FeatureFlags` section
- A public API endpoint (`GET /api/feature-flags`) exposes all configured flags as a JSON object
- No authentication is required to read feature flags
- Flags default to `false` if not configured (unknown flags are treated as disabled)
- The frontend fetches feature flags on application initialization and caches them for the session (`useFeatureFlags()` composable)
- Feature flag removal is a deliberate decision made at the end of a feature's development cycle (CLAUDE.md Gate 8)

## How to add a flag

1. Add a key under `FeatureFlags` in `appsettings.Development.json` (enabled in dev only)
2. Wrap new UI with `v-if="useFeatureFlags().isEnabled('MyFlag')"`
3. Wrap backend behavior with a feature-flag check via `IFeatureFlagService` (or equivalent)
4. Remove the flag when the feature is ready for all environments (CLAUDE.md Gate 8)
