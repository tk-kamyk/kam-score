# Feature Flags

- The system should support feature flags to control visibility of features across environments
- Feature flags are configured in appsettings as flat boolean key-value pairs
- A public API endpoint (`GET /api/feature-flags`) exposes all configured flags as a JSON object
- No authentication is required to read feature flags
- Flags default to `false` if not configured (i.e., unknown flags are treated as disabled)
- The frontend fetches feature flags on application initialization and caches them for the session
- Feature flags are used during development to hide incomplete features behind a flag enabled only in Development configuration
- Feature flag removal is a deliberate decision made at the end of a feature's development cycle (Gate 8)
