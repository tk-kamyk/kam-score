# dev-toolbox

A personal, stack-manifest-driven Claude Code toolbox. Ships generic process
discipline (seven-gate pipeline, orchestrator routing, feedback capture) plus
framework knowledge for .NET, Next.js, Expo, and Turborepo — and a small set of
guard hooks that enforce the conventions automatically.

This file is **for humans** (you, the user). Claude reads `CLAUDE.md` in the
plugin root and individual skill files; run `/toolbox` in any project to see
the live inventory.

## Big picture

```
┌──────────────────────────────────────────────────────────────────────┐
│  USER prompt                                                         │
│    │                                                                  │
│    ▼                                                                  │
│  generic-orchestrator-routing  (substantive? → orchestrator)          │
│    │                                                                  │
│    ▼                                                                  │
│  /agentic-dev-team:orchestrator  (plugin — three-phase workflow)      │
│    │                                                                  │
│    │  reads stack manifest (project's CLAUDE.md fenced YAML)          │
│    │  filters skills whose metadata.stack ⊆ enabled stacks            │
│    ▼                                                                  │
│  agentic-dev-team team agents + filtered dev-toolbox skills        │
│    │   architect, software-engineer, qa-engineer, …                   │
│    │   dotnet-*, nextjs-*, expo-*, generic-* skills                   │
│    ▼                                                                  │
│  generic-gate-pipeline  (Gate 1 → 2 → 3 → 4 → 5 → 6 → 7)              │
│    │                                                                  │
│    ▼                                                                  │
│  /agentic-dev-team:code-review  (review agents per touched file)      │
│    │                                                                  │
│    ▼                                                                  │
│  Gate 7 → generic-feedback-capture  (corrections → skills / rules)    │
└──────────────────────────────────────────────────────────────────────┘
```

The toolbox is split deliberately. **The `agentic-dev-team` plugin** ships the
team — persona agents, the three-phase workflow, review pipeline, model
routing, audit infrastructure. It doesn't know your project. **`dev-toolbox`**
adds stack-manifest-driven framework knowledge and the routing logic that filters
which skills apply on each task. **Your project's `.claude/`** (if you keep one)
carries project-specific bits the plugin shouldn't know about — domain glossary,
vendor-specific integrations, etc.

The **stack manifest** in your project's `CLAUDE.md` is the single source of
truth for what stacks the project has. The orchestrator reads it on every task;
skills whose `metadata.stack` doesn't intersect enabled stacks are filtered out.

## How a request flows (worked example)

User says: *"Add a daily limit per <entity>."*

1. **Routing.** `generic-orchestrator-routing` decides: substantive (new
   behaviour, multiple files, tests required). Invoke
   `/agentic-dev-team:orchestrator`.
2. **Stack scoping.** Orchestrator reads the manifest. Touches API (limit
   enforcement) and SPA (admin UI). Enabled stacks: `dotnet`, `nextjs`,
   `react`, `tailwind`, …
3. **Skill filtering.** Loads `generic-gate-pipeline`,
   `generic-spec-authoring`, `dotnet-clean-architecture`, `dotnet-api-design`,
   `dotnet-data-access`, `nextjs-frontend-standards`. Skips `expo-*` (not in
   scope).
4. **Gate 1 — Requirements.** Drafts a requirement file under
   `docs/requirements/`.
5. **Gate 2 — BDD.** Adds Gherkin scenarios under `docs/bdd/`.
6. **Gate 3 — Mocked UI.** SPA component with hardcoded data.
7. **Gate 4 — Failing tests.** xUnit + Vitest skeletons; all red.
8. **Gate 5 — Implementation.** Tests go green. Self-review via
   `/agentic-dev-team:code-review --changed`.
9. **Gate 6 — Connect UI to API.** Replace mocks with BFF calls.
10. **Gate 7 — Cleanup + feedback capture.** "Next time, do X" gets routed
    through `generic-feedback-capture` into the right skill.
11. **PR.** `/agentic-dev-team:pr` (full pre-PR quality gate) or your project's
    own create-pr command.

## Skill taxonomy

| Prefix | Count | Loads when | What it covers |
|---|---|---|---|
| `generic-*` | 8 | Always | gate-pipeline, orchestrator-routing, code-quality, docs-standards, spec-authoring, memory-policy, claudemd-authoring, feedback-capture |
| `dotnet-*` | 6 | `dotnet` enabled | clean-architecture, api-design, vendor-adapters, coding-patterns, data-access, build-and-runtime |
| `nextjs-*` | 6 | `nextjs` enabled | frontend-standards, ui-implementation, vercel-react-best-practices, sentry, env-var, verify-chrome |
| `expo-*` | 8 | `expo` enabled | native-data-fetching, building-native-ui, deployment, dev-client, tailwind-setup, upgrading, cicd-workflows, use-dom |
| `turborepo-*` | 1 | `turborepo` enabled | conventions |
| `azure-*` | 1 | `azure` enabled | devops-pr-format |

What's intentionally **not** here: vendor-specific skills (Adyen, Auth0,
Signicat, etc.), project glossaries, domain models. Those belong in your
project's `.claude/skills/` and `.claude/rules/` so they only load for the
project that needs them.

## Hook catalog

All hooks live in `<plugin>/hooks/`. They read the consuming project's
`CLAUDE.md` stack manifest and exit 0 silently when the relevant stack isn't
enabled — so unrelated work isn't slowed down.

| Hook | Event | What it does |
|---|---|---|
| `session-start-pulse.sh` | SessionStart | Per-stack heartbeats — git status vs main, uncommitted changes, last commit, `.NET` / Next.js / Expo versions for each enabled stack |
| `stop-test-reminder.sh` | Stop | Nags if `.cs` / `.ts` / `.tsx` were modified without running tests |
| `guard-pr-format.sh` | PreToolUse on Azure DevOps PR mcps | Enforces `type(topic): Description` title format; rejects literal `\n` in body |
| `guard-private-folders.sh` + `*-bash.sh` | PreToolUse on Write/Edit/Bash | Forces underscore prefix on non-route folders in `app/` (Next.js App Router) |
| `guard-process-env.sh` | PreToolUse on Write/Edit | Blocks raw `process.env`; forces `import { env } from '@/lib/env'` |
| `guard-frontend-query-caching.sh` | PreToolUse on Write/Edit | Blocks per-query `staleTime` / `gcTime` overrides; cache config lives globally |
| `guard-stack-manifest.sh` | PreToolUse on Write/Edit of CLAUDE.md | Validates the fenced YAML manifest; blocks on parse failure |

## Slash commands

| Command | Purpose |
|---|---|
| `/orchestrator` | Alias to `/agentic-dev-team:orchestrator` |
| `/toolbox` | Live inventory of commands, skills, hooks, plugin version |
| `/check` | Per-stack test/lint matrix from the manifest |
| `/affected` | Turborepo `--affected` change scope |
| `/generate-api` | Regen Orval client; run check-types |
| `/env-status` | Cross-stack env-file health |
| `/learn` | Deliberate feedback capture via `generic-feedback-capture` |

Plus all `/agentic-dev-team:*` commands shipped by the `agentic-dev-team` plugin.

## How to extend

**Add a skill** — pick a stack prefix (`<stack>-<topic>` or `generic-<topic>`),
create `<plugin>/skills/<name>/SKILL.md`. Frontmatter needs `name`,
`description`, and `metadata.stack: [<tag>, ...]`. Read
`generic-claudemd-authoring` (general principles) and
`agentic-dev-team:agent-skill-authoring` (plugin conventions) first.

**Add a hook** — script in `<plugin>/hooks/`, `chmod +x`, then add an entry to
`<plugin>/settings.json` under the matching event and matcher. Use the existing
hooks as templates — they read stdin JSON and exit non-zero to block.

**Add a slash command** — file in `<plugin>/commands/<name>.md`. Frontmatter
`user-invocable: true` and an optional `argument-hint`. Body is the prompt the
command expands to.

**For incremental learning during a session** — use `/learn <thing>` instead of
writing a new skill from scratch. It routes the feedback to the right destination
(skill / rule / glossary / auto-memory / decisions.md) and proposes a new skill
only when the fact doesn't fit anywhere.
