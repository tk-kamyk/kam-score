---
name: toolbox
description: Self-introspection. Lists local commands, local skills with stack tags, the active stack manifest, plugin skills filtered by manifest, and the agentic-dev-team plugin version. Pure read.
user-invocable: true
---

Inspect the `.claude/` toolbox and report the inventory. Do NOT modify anything.

## Steps

1. **Read the stack manifest** from `CLAUDE.md`. Look for the fenced ```yaml block tagged `# stack-manifest`. Parse with `yq` or by inspection. Compute the set of enabled stacks (where `enabled: true`).

2. **List local commands** in `.claude/commands/`:
   ```
   ls .claude/commands/
   ```
   For each, read the frontmatter `description:` and report.

3. **List local skills** in `.claude/skills/`. For each skill:
   - Read the frontmatter `name:`, `description:`, and `metadata.stack:`.
   - Mark whether the skill will load given the current manifest:
     - **active** — `metadata.stack` ⊆ enabled stacks, OR `metadata.stack == [generic]`, OR `metadata.required: true`.
     - **filtered** — has stack tags that aren't all enabled in the manifest.
   - Group output by stack prefix: `generic-*`, `dotnet-*`, `nextjs-*`, `expo-*`, `vendor-*`, `azure-*`, `turborepo-*`.

4. **List local hooks** wired in `.claude/settings.json`. For each event (SessionStart, PreToolUse, Stop), list the hook command paths.

5. **Plugin version**:
   ```
   ls -d ~/.claude/plugins/cache/bfinster/agentic-dev-team/*/ 2>/dev/null | tail -1
   ```
   Report the version directory name.

6. **Plugin skills filtered by manifest**: this is best-effort — the plugin's skills don't carry our `metadata.stack` tags. Skip if no easy way to filter; report the plugin's skill count from `~/.claude/plugins/cache/bfinster/agentic-dev-team/*/skills/`.

## Output format

```
=== Continia.Card toolbox ===
Plugin: agentic-dev-team v<version>
Active stacks: <comma-separated tags>

Local commands (.claude/commands/):
  /<name>    <description>
  ...

Local skills (.claude/skills/):
  generic-*:
    [active]   <name>    <description>
    ...
  dotnet-*:
    [active]   <name>    <description>
    ...
  (filtered out — stack not in manifest):
    [filtered] <name>    stack=<tags>
    ...

Hooks (settings.json):
  SessionStart:
    .claude/hooks/session-start-pulse.sh
  PreToolUse (<matcher>):
    .claude/hooks/<file>
  ...
```

## Notes

- Pure read. Do not edit files, do not run commits, do not spawn agents.
- If the manifest cannot be parsed, print the parse error and a hint to run `.claude/hooks/guard-stack-manifest.sh` against the file.
- Pass `$ARGUMENTS` through unchanged — currently unused, reserved for future filtering (e.g. `/toolbox skills` or `/toolbox audit`).
