#!/bin/bash
# Validate the fenced stack-manifest YAML inside CLAUDE.md.
#
# Usage: validate-manifest.sh <path-to-CLAUDE.md>
# Exit:  0 = valid (or syntactic-only valid), 1 = no manifest found,
#        2 = YAML parse error, 3 = schema error.
#
# Uses yq if available for full YAML validation; otherwise an awk-based
# check verifies the block shape (top-level `stacks:` map, each stack has
# an `enabled:` key).

set -euo pipefail

CLAUDE_MD="${1:-CLAUDE.md}"

if [[ ! -f "$CLAUDE_MD" ]]; then
  echo "ERROR: $CLAUDE_MD not found" >&2
  exit 1
fi

# --- Extract the fenced ```yaml block tagged "# stack-manifest" -----------
MANIFEST_YAML=$(awk '
  /^```yaml[[:space:]]*$/ { in_fence=1; buf=""; next }
  in_fence && /^```[[:space:]]*$/ { if (has_marker) { print buf } in_fence=0; has_marker=0; next }
  in_fence {
    if ($0 ~ /^#[[:space:]]*stack-manifest/) has_marker=1
    buf = buf $0 ORS
  }
' "$CLAUDE_MD")

if [[ -z "$MANIFEST_YAML" ]]; then
  echo "ERROR: no fenced '# stack-manifest' YAML block found in $CLAUDE_MD" >&2
  exit 1
fi

# --- Try yq if available --------------------------------------------------
HAS_YQ=0
if command -v yq >/dev/null 2>&1; then HAS_YQ=1; fi

if [[ "$HAS_YQ" == 1 ]]; then
  STACKS_COUNT=$(echo "$MANIFEST_YAML" | yq '.stacks | length' 2>/dev/null || echo "0")
  if [[ "$STACKS_COUNT" -lt 1 ]]; then
    echo "ERROR: manifest must have a non-empty 'stacks:' map" >&2
    exit 3
  fi
  MISSING_ENABLED=$(echo "$MANIFEST_YAML" | yq '.stacks | to_entries | map(select(.value.enabled == null)) | .[].key' 2>/dev/null | tr -d '"')
  if [[ -n "$MISSING_ENABLED" ]]; then
    echo "ERROR: stacks missing 'enabled:' key:" >&2
    echo "$MISSING_ENABLED" | sed 's/^/  - /' >&2
    exit 3
  fi
  ENABLED=$(echo "$MANIFEST_YAML" | yq '.stacks | to_entries | map(select(.value.enabled == true)) | .[].key' 2>/dev/null | tr -d '"' | tr '\n' ' ')
  echo "OK: $STACKS_COUNT stacks declared, enabled: $ENABLED"
  exit 0
fi

# --- Fallback: awk-based syntactic + schema check -------------------------
TMP=$(mktemp)
printf '%s' "$MANIFEST_YAML" > "$TMP"
trap 'rm -f "$TMP"' EXIT

awk '
  /^[[:space:]]*$/ { next }
  /^[[:space:]]*#/ { next }
  /^stacks:[[:space:]]*$/ { in_stacks = 1; next }
  !in_stacks { next }
  /^  [a-zA-Z][a-zA-Z0-9_-]*:/ {
    declared++
    name=$1; sub(":","",name)
    seen[name]=1
    last_name=name
    if ($0 ~ /enabled:[[:space:]]*(true|false)/) {
      has_enabled[name]=1
      if ($0 ~ /enabled:[[:space:]]*true/) enabled_count++
    }
    next
  }
  /^    enabled:[[:space:]]*true/ {
    enabled_count++
    if (last_name) has_enabled[last_name]=1
    next
  }
  /^    enabled:[[:space:]]*false/ {
    if (last_name) has_enabled[last_name]=1
    next
  }
  END {
    if (declared == 0) {
      print "ERROR: no stacks declared" > "/dev/stderr"
      exit 3
    }
    schema_err = 0
    for (s in seen) {
      if (!(s in has_enabled)) {
        printf("ERROR: stack %s missing enabled: key\n", s) > "/dev/stderr"
        schema_err = 1
      }
    }
    if (schema_err) exit 3
    printf("OK: %d stacks declared, %d enabled\n", declared, enabled_count)
  }
' "$TMP"
