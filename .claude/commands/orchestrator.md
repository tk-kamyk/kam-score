---
name: orchestrator
description: >-
  Project-local alias for /agentic-dev-team:orchestrator. Required because CLAUDE.md
  names the plugin orchestrator as the entry point for all substantive work, and we
  want typed `/orchestrator` to route deterministically instead of being interpreted
  by the model as freeform text.
argument-hint: "<request>"
user-invocable: true
---

Invoke the Skill `agentic-dev-team:orchestrator` immediately with the arguments below. Do not search the repo, do not answer inline, do not spawn any other agent first — the plugin orchestrator is the authoritative entry point per CLAUDE.md.

Arguments: $ARGUMENTS
