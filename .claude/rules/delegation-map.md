# Delegation map

Human-only reference. Names which `agentic-dev-team` plugin agent, skill, or command handles each kind of intent. Read this when extending the toolbox or wondering "is there already a plugin entry for this before I write a local one?"

## Phase work

| Intent | Plugin entry |
|---|---|
| Start a feature (route + multi-phase) | `/agentic-dev-team:orchestrator` |
| Draft an implementation plan | `/agentic-dev-team:plan` |
| Execute an approved plan with TDD | `/agentic-dev-team:build` |
| Produce the four spec artifacts | `/agentic-dev-team:specs` |
| Write a design doc before planning | `/agentic-dev-team:design-doc` |
| Finalise + merge a feature branch | `/agentic-dev-team:branch-workflow` |
| Resume in-progress work | `/agentic-dev-team:continue` |
| Triage a bug into an issue | `/agentic-dev-team:triage` |
| Root-cause investigation | `/agentic-dev-team:systematic-debugging`, `/agentic-dev-team:root-why` |
| CI failure diagnosis | `/agentic-dev-team:ci-debugging` |

## Reviews

| Intent | Plugin entry |
|---|---|
| Full review of changed files | `/agentic-dev-team:code-review --changed` |
| Single review agent (e.g. security) | `/agentic-dev-team:review-agent <name>` |
| Static analysis pre-pass | `/agentic-dev-team:semgrep-analyze` |
| Mutation testing | `/agentic-dev-team:mutation-testing` |
| Apply correction prompts | `/agentic-dev-team:apply-fixes` |

## Design

| Intent | Plugin entry |
|---|---|
| Strategic DDD (bounded contexts) | `/agentic-dev-team:domain-driven-design` |
| Domain health assessment | `/agentic-dev-team:domain-analysis` |
| Hexagonal architecture | `/agentic-dev-team:hexagonal-architecture` |
| Contract-first API design | `/agentic-dev-team:api-design` |
| Threat modeling (STRIDE) | `/agentic-dev-team:threat-modeling` |
| Stress-test a plan | `/agentic-dev-team:design-interrogation` |

## Infra

| Intent | Plugin entry |
|---|---|
| Generate a Dockerfile | `/agentic-dev-team:docker-image-create` |
| Audit a Docker image | `/agentic-dev-team:docker-image-audit` |
| Bootstrap a new JS project | `/agentic-dev-team:js-project-init` |
| Add a Claude Code plugin | `/agentic-dev-team:add-plugin` |
| Upgrade plugins | `/agentic-dev-team:upgrade` |

## What local skills add

The plugin doesn't know our project. Local skills (under `.claude/skills/`) carry the KamScore specifics: the Phase Format Strategy triad, three-tier auth model, KamScore domain services, and the Vue/Vuetify frontend conventions. When a plugin agent runs, it loads matching local skills via stack-tag filtering — see the manifest in `CLAUDE.md`.

## When to write a local entry vs use the plugin

- **Use the plugin** if it already covers the intent. Don't wrap it.
- **Write a local skill** if you have project-specific knowledge that should load when working in a particular area (file paths, frontmatter triggers, stack-tag intersect).
- **Write a local slash command** if you want a verb that delegates with project context pre-loaded (e.g. `/check` runs the per-stack matrix from the manifest).
- **Don't write a local agent** unless there's a real persona gap the plugin doesn't fill. Wrappers drift on plugin upgrades.
