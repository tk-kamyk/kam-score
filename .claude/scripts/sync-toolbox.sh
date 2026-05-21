#!/bin/bash
# sync-toolbox.sh — lift this .claude/ toolbox into another project.
#
# Usage: bash .claude/scripts/sync-toolbox.sh <target-project-root>
#
# What it does:
# 1. Validates the source manifest in the source CLAUDE.md.
# 2. Prompts the user to enable/disable stacks for the target.
# 3. Backs up <target>/.claude if non-empty.
# 4. Copies always-needed pieces (generic-* skills, entry-point command,
#    delegation-map rule, scripts, prompts, templates, base hooks).
# 5. Per enabled stack, copies matching skills/hooks/commands by reading
#    each file's metadata.stack frontmatter.
# 6. Emits <target>/.claude/CLAUDE.md.suggested for the user to merge.
# 7. Merges settings.json hook arrays where the referenced files now exist.
# 8. Logs the sync to <target>/.claude/.sync-log.
#
# Dependencies: bash (with awk + sed). No python, no jq.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_CLAUDE_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SOURCE_REPO="$(cd "$SOURCE_CLAUDE_DIR/.." && pwd)"
SOURCE_CLAUDE_MD="$SOURCE_REPO/CLAUDE.md"

TARGET_REPO="${1:-}"
if [[ -z "$TARGET_REPO" ]]; then
  echo "Usage: $0 <target-project-root>" >&2
  exit 1
fi
if [[ ! -d "$TARGET_REPO" ]]; then
  echo "ERROR: $TARGET_REPO is not a directory" >&2
  exit 1
fi
TARGET_REPO="$(cd "$TARGET_REPO" && pwd)"

if [[ "$TARGET_REPO" == "$SOURCE_REPO" ]]; then
  echo "ERROR: target is the same as source" >&2
  exit 1
fi

# --- 1. Validate source manifest -------------------------------------------

if ! bash "$SCRIPT_DIR/validate-manifest.sh" "$SOURCE_CLAUDE_MD" >/dev/null; then
  echo "ERROR: source manifest in $SOURCE_CLAUDE_MD is invalid" >&2
  exit 1
fi

# Extract stack names from the source manifest as the *suggested* set.
# The manifest is a fenced ```yaml block tagged with "# stack-manifest";
# inside it, each stack is a two-space-indented key like:
#   dotnet:       { enabled: true, ... }
SOURCE_STACKS=$(awk '
  /^```yaml[[:space:]]*$/ { in_fence=1; in_stacks=0; next }
  in_fence && /^```[[:space:]]*$/ { if (has_marker) exit; in_fence=0; next }
  in_fence && /^#[[:space:]]*stack-manifest/ { has_marker=1; next }
  !has_marker { next }
  in_fence && /^stacks:[[:space:]]*$/ { in_stacks=1; next }
  in_stacks && /^  [a-zA-Z][a-zA-Z0-9_-]*:/ {
    name=$1; sub(":","",name); print name
  }
' "$SOURCE_CLAUDE_MD")

# --- 2. Interactive stack selection ----------------------------------------

echo "Source toolbox detected stacks:"
echo "$SOURCE_STACKS" | nl -ba

# Optionally pre-probe the target
echo
echo "Detected stacks in target ($TARGET_REPO):"
bash "$SCRIPT_DIR/detect-stacks.sh" "$TARGET_REPO" || true

echo
echo "Which stacks should be enabled in the target? Enter space-separated names"
echo "(default = all detected source stacks). Type 'none' for only generic."
read -r -p "Stacks: " USER_INPUT
if [[ -z "$USER_INPUT" ]]; then
  ENABLED_STACKS=$(echo "$SOURCE_STACKS")
elif [[ "$USER_INPUT" == "none" ]]; then
  ENABLED_STACKS="generic"
else
  ENABLED_STACKS=$(echo "$USER_INPUT" | tr ' ' '\n')
fi

# 'generic' is always implicitly enabled — it carries always-applicable skills.
if ! echo "$ENABLED_STACKS" | grep -qx generic; then
  ENABLED_STACKS=$(printf 'generic\n%s\n' "$ENABLED_STACKS")
fi

echo
echo "Will enable in target:"
echo "$ENABLED_STACKS" | sed 's/^/  - /'
echo
read -r -p "Proceed? [Y/n] " CONFIRM
if [[ "$CONFIRM" =~ ^[Nn] ]]; then
  echo "Aborted." >&2
  exit 1
fi

# --- 3. Backup existing target/.claude -------------------------------------

TARGET_CLAUDE="$TARGET_REPO/.claude"
if [[ -d "$TARGET_CLAUDE" ]] && [[ -n "$(ls -A "$TARGET_CLAUDE" 2>/dev/null)" ]]; then
  STAMP=$(date +%Y%m%d-%H%M%S)
  BACKUP="$TARGET_REPO/.claude.bak.$STAMP"
  cp -R "$TARGET_CLAUDE" "$BACKUP"
  echo "Backed up existing $TARGET_CLAUDE -> $BACKUP"
fi
mkdir -p "$TARGET_CLAUDE"/{skills,commands,hooks,rules,prompts,scripts,templates}

# --- 4. Always-copy plumbing -----------------------------------------------

# Always: skills/generic-*, commands/orchestrator|toolbox|sync-toolbox, rules,
#         prompts, templates, scripts, base hooks (session-start, guard-stack-manifest)

# generic skills — strip trailing slash so BSD cp copies the dir, not its contents
for d in "$SOURCE_CLAUDE_DIR/skills/"generic-*/; do
  [[ -d "$d" ]] || continue
  cp -R "${d%/}" "$TARGET_CLAUDE/skills/"
done

# entry-point + introspection commands
for c in orchestrator.md toolbox.md sync-toolbox.md; do
  [[ -f "$SOURCE_CLAUDE_DIR/commands/$c" ]] && cp "$SOURCE_CLAUDE_DIR/commands/$c" "$TARGET_CLAUDE/commands/"
done

# rules + scripts + prompts + templates as-is
cp -R "$SOURCE_CLAUDE_DIR/rules/." "$TARGET_CLAUDE/rules/" 2>/dev/null || true
cp -R "$SOURCE_CLAUDE_DIR/scripts/." "$TARGET_CLAUDE/scripts/" 2>/dev/null || true
cp -R "$SOURCE_CLAUDE_DIR/prompts/." "$TARGET_CLAUDE/prompts/" 2>/dev/null || true
cp -R "$SOURCE_CLAUDE_DIR/templates/." "$TARGET_CLAUDE/templates/" 2>/dev/null || true

# base hooks
for h in session-start-pulse.sh guard-stack-manifest.sh; do
  [[ -f "$SOURCE_CLAUDE_DIR/hooks/$h" ]] && cp "$SOURCE_CLAUDE_DIR/hooks/$h" "$TARGET_CLAUDE/hooks/"
done

# --- 5. Per-enabled-stack copy ---------------------------------------------

# For each enabled stack tag, iterate over all skills/commands/hooks and copy
# any whose metadata.stack contains the tag.
copy_by_stack_tag() {
  local kind="$1"; shift   # skills | commands | hooks
  local tag="$1"; shift
  local src_dir="$SOURCE_CLAUDE_DIR/$kind"
  [[ -d "$src_dir" ]] || return 0

  for entry in "$src_dir"/*; do
    local name="$(basename "$entry")"
    # Skip already-copied generic items
    case "$kind:$name" in
      skills:generic-*|commands:orchestrator.md|commands:toolbox.md|commands:sync-toolbox.md|hooks:session-start-pulse.sh|hooks:guard-stack-manifest.sh)
        continue ;;
    esac

    # Determine the file to inspect for frontmatter
    local meta_file=""
    if [[ -d "$entry" && -f "$entry/SKILL.md" ]]; then meta_file="$entry/SKILL.md"
    elif [[ -f "$entry" ]]; then meta_file="$entry"
    else continue
    fi

    # Match tag against metadata.stack in the file's YAML frontmatter.
    # Supported shapes:
    #   metadata:
    #     stack: [tag1, tag2]      # flow list
    #     stack: tag1              # scalar
    #     required: true           # always-match shortcut
    local matches
    matches=$(awk -v tag="$tag" '
      NR==1 && /^---[[:space:]]*$/ { in_fm=1; next }
      in_fm && /^---[[:space:]]*$/ { exit }
      !in_fm { exit }
      /^metadata:[[:space:]]*$/ { in_meta=1; next }
      in_meta && /^[a-zA-Z]/ { in_meta=0 }
      in_meta && /^[[:space:]]+required:[[:space:]]*true/ { found=1; exit }
      in_meta && /^[[:space:]]+stack:/ {
        line=$0
        sub(/^[[:space:]]+stack:[[:space:]]*/, "", line)
        gsub(/[\[\]",]/, " ", line)
        n=split(line, parts, /[[:space:]]+/)
        for (i=1; i<=n; i++) if (parts[i]==tag) { found=1; exit }
      }
      END { if (found) print "MATCH" }
    ' "$meta_file")
    if [[ "$matches" == "MATCH" ]]; then
      if [[ -d "$entry" ]]; then
        cp -R "$entry" "$TARGET_CLAUDE/$kind/"
      else
        cp "$entry" "$TARGET_CLAUDE/$kind/"
      fi
    fi
  done
}

while read -r tag; do
  [[ -z "$tag" ]] && continue
  # For tag='generic', the copy_by_stack_tag skip list (skills:generic-*, base
  # commands/hooks) ensures we don't re-copy what always-copy already handled;
  # we still pick up non-prefixed items tagged metadata.stack: [generic].
  copy_by_stack_tag "skills" "$tag"
  copy_by_stack_tag "commands" "$tag"
  copy_by_stack_tag "hooks" "$tag"
done <<< "$ENABLED_STACKS"

# --- 6. CLAUDE.md.suggested -------------------------------------------------

SUGGESTED="$TARGET_CLAUDE/CLAUDE.md.suggested"
{
  echo "# CLAUDE.md"
  echo
  echo "(Suggested skeleton emitted by sync-toolbox on $(date -u +'%Y-%m-%dT%H:%M:%SZ').)"
  echo "(Merge with your existing CLAUDE.md; do NOT overwrite blindly.)"
  echo
  echo "## Agent Team and Entry Point"
  echo
  echo "Substantive engineering work routes through \`/agentic-dev-team:orchestrator\`."
  echo "Bare \`/orchestrator\` is a project-local alias. See \`generic-orchestrator-routing\` skill for the threshold."
  echo
  echo "## Stack Manifest (single source of truth)"
  echo
  echo "\`\`\`yaml"
  echo "# stack-manifest"
  echo "stacks:"
  while read -r tag; do
    [[ -z "$tag" ]] && continue
    echo "  $tag: { enabled: true }"
  done <<< "$ENABLED_STACKS"
  echo "\`\`\`"
  echo
  echo "## Memory"
  echo "See \`generic-memory-policy\` skill."
  echo
  echo "## Process"
  echo "Seven-gate process — see \`generic-gate-pipeline\` skill."
  echo
  echo "## Discover the toolbox"
  echo "Run \`/toolbox\` to see what's available."
} > "$SUGGESTED"

echo
echo "Emitted $SUGGESTED — merge into your CLAUDE.md."

# --- 7. settings.json merge (lightweight) ----------------------------------

# If target has settings.json, leave it alone; otherwise copy a starter from source.
if [[ ! -f "$TARGET_CLAUDE/settings.json" ]] && [[ -f "$SOURCE_CLAUDE_DIR/settings.json" ]]; then
  cp "$SOURCE_CLAUDE_DIR/settings.json" "$TARGET_CLAUDE/settings.json"
  echo "Copied starter settings.json. Review the hook paths and remove entries for hooks that weren't copied to the target."
else
  echo "Existing target settings.json left alone. Manually merge hook arrays for newly-installed hooks if desired."
fi

# --- 8. Audit log -----------------------------------------------------------

SOURCE_SHA=$(cd "$SOURCE_REPO" && git rev-parse --short HEAD 2>/dev/null || echo "unknown")
{
  echo "$(date -u +'%Y-%m-%dT%H:%M:%SZ')  sync from=$SOURCE_REPO sha=$SOURCE_SHA  stacks=$(echo "$ENABLED_STACKS" | tr '\n' ',' | sed 's/,$//')"
} >> "$TARGET_CLAUDE/.sync-log"

echo
echo "=== Sync complete ==="
echo "  Target: $TARGET_CLAUDE"
echo "  Stacks: $(echo "$ENABLED_STACKS" | tr '\n' ',' | sed 's/,$//')"
echo "  Next:   review CLAUDE.md.suggested and merge into your CLAUDE.md."
echo "  Next:   open Claude Code in the target and run /toolbox to verify."
