---
name: sync-toolbox
description: Lift this `.claude/` toolbox into another project. Wraps scripts/sync-toolbox.sh. Prompts for stack selection so only matching skills/hooks/commands land in the target.
argument-hint: "<target-project-root>"
user-invocable: true
---

Lift this `.claude/` toolbox into another project. Always copies `generic-*` skills + the always-needed plumbing; per-stack content (`dotnet-*`, `nextjs-*`, `expo-*`, `vendor-*`, `azure-*`, `turborepo-*`) is copied only for stacks the user enables for the target.

## Steps

1. **Resolve target**:
   - If `$ARGUMENTS` provided, use it as the target project root.
   - Otherwise, ask the user.
   - Refuse with an error if the target is the same as the source.

2. **Invoke the sync script** (it does the interactive heavy lifting):
   ```bash
   bash .claude/scripts/sync-toolbox.sh $ARGUMENTS
   ```

3. **After the script returns**:
   - Confirm the target now has a `.claude/` populated per the chosen stacks.
   - Point the user at `<target>/.claude/CLAUDE.md.suggested` — they merge this into their own CLAUDE.md (the script never overwrites the target's CLAUDE.md).
   - Point at `<target>/.claude/.sync-log` for the audit entry.

4. **If the script reports an error** (e.g. yq/jq missing on the system), surface the error verbatim and recommend an install command per the target OS.

## What's always copied

- `skills/generic-*/`
- `commands/orchestrator.md`, `commands/toolbox.md`, `commands/sync-toolbox.md`
- `rules/delegation-map.md`, `rules/project-glossary.md` (template; user will customise)
- `prompts/`, `templates/`, `scripts/`
- `hooks/session-start-pulse.sh`, `hooks/guard-stack-manifest.sh`

## What's stack-conditional

| Stack tag | Copies if enabled |
|---|---|
| `dotnet` | `skills/dotnet-*`, `hooks/guard-no-result-on-task.sh`, etc. (when those land) |
| `nextjs` | `skills/nextjs-*`, `hooks/guard-process-env.sh`, `hooks/guard-frontend-query-caching.sh`, `hooks/guard-private-folders*.sh`, `commands/generate-api.md`, `commands/affected.md` |
| `expo` | `skills/expo-*` |
| `vendor: adyen` | `skills/vendor-adyen-integration` |
| `vendor: signicat` | `skills/vendor-signicat-*` |
| `vendor: auth0` | `skills/vendor-auth0-bff` |
| `azure` | `skills/azure-*`, `hooks/guard-pr-format.sh`, `commands/create-pr.md`, `commands/work-status.md` |
| `turborepo` | `skills/turborepo-conventions`, `commands/affected.md` |

The script reads `metadata.stack` from each skill/hook/command frontmatter to decide.

## Notes

- This command is non-destructive on the target's existing files — the script backs up any pre-existing `.claude/` and emits a `CLAUDE.md.suggested` for manual merge.
- For the source-side maintenance flow (updating this toolbox), commit changes to a feature branch in the source repo and re-run `/sync-toolbox <target>` for each consuming project.
