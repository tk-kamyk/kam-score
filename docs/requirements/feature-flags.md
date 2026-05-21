# Feature Flags (mechanism)

**Status**: No active flags today. This plumbing is kept as intentional boilerplate so new features can be gated behind a flag during development without re-introducing the infrastructure each time.

## Mechanism

- [FR-FF-001] Feature flags are configured in appsettings as flat boolean key-value pairs under the `FeatureFlags` section.
- [FR-FF-002] A public API endpoint (`GET /api/feature-flags`) exposes all configured flags as a JSON object; no authentication is required.
- [FR-FF-003] Unknown flags are treated as disabled (default `false`).
- [FR-FF-004] The frontend fetches feature flags on application initialisation and caches them for the session.
- [FR-FF-005] Feature-flag removal is a deliberate decision made at the end of a feature's development cycle.

## How to add a flag

1. Add a key under `FeatureFlags` in `appsettings.Development.json` (enabled in dev only).
2. Wrap new UI with `v-if="useFeatureFlags().isEnabled('MyFlag')"`.
3. Wrap backend behaviour with a feature-flag service check.
4. Remove the flag when the feature is ready for all environments.
