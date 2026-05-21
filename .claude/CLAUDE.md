# dev-toolbox

A personal Claude Code toolbox. Stack-manifest-driven: the consuming project's
`CLAUDE.md` declares which stacks are enabled, and the orchestrator filters skills
and hooks based on that manifest.

Plugin-shipped skills are knowledge only. The plugin defines no agents; agent
personas come from `agentic-dev-team`. Substantive work routes through that
plugin's orchestrator.

## Agent team and entry point

Substantive engineering work routes through `/agentic-dev-team:orchestrator`.
A bare `/orchestrator` is a project-local alias to the same. Detail and the
substantive-vs-trivial threshold live in the `generic-orchestrator-routing` skill.

## Development process

Seven gates in order — see `generic-gate-pipeline`. Do not start coding before
Gate 4. Stack-conditional skipping is described in the same skill.

## Memory

Two layers, governed separately. See `generic-memory-policy`:

- User-level auto-memory at `~/.claude/projects/<slug>/memory/` — persistent across
  sessions; harness-managed.
- Project-local `memory/` — used by `agentic-dev-team` for phase progress and
  `decisions.md`.

## Required: project-side stack manifest

Each consuming project's `CLAUDE.md` must contain a fenced YAML block, marked
with the `# stack-manifest` comment, listing enabled stacks. Example:

````
```yaml
# stack-manifest
stacks:
  generic:   { enabled: true }
  dotnet:    { enabled: true, root: api/,        test_cmd: "dotnet test api/<Solution>.slnx" }
  nextjs:    { enabled: true, root: spa/,        test_cmd: "cd spa && pnpm check-types && pnpm lint && pnpm test" }
  react:     { enabled: true }
  tailwind:  { enabled: true }
  expo:      { enabled: true, root: mobile-app/, test_cmd: "cd mobile-app && pnpm lint && pnpm test" }
  turborepo: { enabled: true }
  azure:     { enabled: true }
```
````

The orchestrator filters skills whose `metadata.stack` ⊄ enabled stacks (with
`generic-*` and `metadata.required: true` always loaded). The `guard-stack-manifest.sh`
hook validates this block on every edit.

## Skill taxonomy

| Prefix | Loads when | What it carries |
|---|---|---|
| `generic-*` | Always | Gate pipeline, orchestrator routing, code quality, docs standards, spec authoring, memory policy, claudemd authoring, feedback capture |
| `dotnet-*` | `dotnet` enabled | Clean Architecture, API design, data access, vendor adapters, coding patterns, build & runtime |
| `nextjs-*` | `nextjs` enabled | Frontend standards, UI implementation, Vercel/React best practices, Sentry, env-var workflow, Chrome verification |
| `expo-*` | `expo` enabled | Native UI, deployment, dev client, data fetching, Tailwind setup, upgrading, CI/CD workflows, use-dom |
| `turborepo-*` | `turborepo` enabled | Pipeline / filter / cache conventions |
| `azure-*` | `azure` enabled | Azure DevOps PR title contract |

## Hook catalog

| Hook | Event | Purpose |
|---|---|---|
| `session-start-pulse.sh` | SessionStart | Per-stack heartbeats from the manifest, env staleness flag |
| `stop-test-reminder.sh` | Stop | Nags if modified implementation files lack a test run |
| `guard-stack-manifest.sh` | PreToolUse / Write\|Edit | Validates the fenced YAML manifest on every CLAUDE.md edit |
| `guard-pr-format.sh` | PreToolUse / ADO PR mcps | Enforces `type(topic): Description` title format |
| `guard-private-folders.sh` + `*-bash.sh` | PreToolUse | Underscore prefix for non-route folders in Next.js App Router |
| `guard-process-env.sh` | PreToolUse / Write\|Edit | Blocks raw `process.env`; forces `import { env } from '@/lib/env'` |
| `guard-frontend-query-caching.sh` | PreToolUse / Write\|Edit | Blocks per-query `staleTime` / `gcTime` overrides |

All hooks are stack-scoped: they read the manifest, exit 0 silently when their
target stack isn't enabled.

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

## Discovery

Run `/toolbox` in any project where the plugin is enabled to see the live
inventory.
