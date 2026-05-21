#!/bin/bash
# Validate the stack-manifest YAML when CLAUDE.md is written/edited.
# Reads PreToolUse input JSON; if the file being edited is CLAUDE.md, runs the
# validator against the new content (extracted from tool_input).

INPUT=$(cat)

FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')
TOOL=$(echo "$INPUT" | jq -r '.tool_name // empty')

# Only act on writes/edits to CLAUDE.md
[[ "$FILE_PATH" != *"CLAUDE.md" ]] && exit 0
[[ "$TOOL" != "Write" && "$TOOL" != "Edit" ]] && exit 0

# Resolve repo root for the script path
REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || echo "")
SCRIPT="$REPO_ROOT/.claude/scripts/validate-manifest.sh"

[[ ! -x "$SCRIPT" ]] && exit 0

# For Write: validate the new content directly (write to a temp file).
# For Edit: we can only validate the on-disk file post-write; the simpler
# approach is to validate the current file before the edit and trust the
# Stop/PostToolUse hooks if it lands wrong. For Write, we validate the
# proposed content.

if [[ "$TOOL" == "Write" ]]; then
  CONTENT=$(echo "$INPUT" | jq -r '.tool_input.content // empty')
  if [[ -n "$CONTENT" ]]; then
    TMP=$(mktemp)
    printf '%s' "$CONTENT" > "$TMP"
    if ! bash "$SCRIPT" "$TMP" >/dev/null 2>&1; then
      bash "$SCRIPT" "$TMP" >&2 || true
      echo "BLOCKED: proposed CLAUDE.md contains an invalid stack-manifest YAML block." >&2
      rm -f "$TMP"
      exit 2
    fi
    rm -f "$TMP"
  fi
else
  # Edit: validate the current on-disk file (best we can do pre-write).
  if [[ -f "$FILE_PATH" ]]; then
    if ! bash "$SCRIPT" "$FILE_PATH" >/dev/null 2>&1; then
      bash "$SCRIPT" "$FILE_PATH" >&2 || true
      echo "WARNING: current CLAUDE.md has an invalid stack-manifest. Edit will proceed; please fix." >&2
      # Do not block edits — the user might be fixing the manifest right now.
    fi
  fi
fi

exit 0
