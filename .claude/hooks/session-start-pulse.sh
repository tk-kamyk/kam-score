#!/bin/bash
# Project Pulse — session-start context for Continia.Card across api/, spa/, mobile-app/.

cd "$(git rev-parse --show-toplevel 2>/dev/null)" || exit 0

echo "=== Project Pulse ==="
echo ""

BRANCH=$(git branch --show-current 2>/dev/null)
echo "Branch: $BRANCH"

# Commits ahead/behind main
git fetch origin main --quiet 2>/dev/null
AHEAD=$(git rev-list origin/main..HEAD --count 2>/dev/null)
BEHIND=$(git rev-list HEAD..origin/main --count 2>/dev/null)
echo "vs main: ${AHEAD:-0} ahead, ${BEHIND:-0} behind"

# Uncommitted changes
CHANGES=$(git status --porcelain 2>/dev/null | wc -l | tr -d ' ')
if [ "$CHANGES" -gt 0 ]; then
  echo "Uncommitted changes: $CHANGES files"
  git status --porcelain 2>/dev/null | head -10
else
  echo "Working tree: clean"
fi

echo ""
echo "Last commit:"
git log -1 --oneline 2>/dev/null

# Stack heartbeats (best-effort; never block on missing tools)
echo ""
echo "Stacks:"
if [ -d api ] && command -v dotnet >/dev/null 2>&1; then
  DOTNET_VER=$(dotnet --version 2>/dev/null)
  echo "  api/      dotnet ${DOTNET_VER:-?}"
fi
if [ -d spa ]; then
  echo "  spa/      Next.js + Turborepo (pnpm)"
fi
if [ -d mobile-app ]; then
  SDK_VER=$(grep -oE '"expo"[[:space:]]*:[[:space:]]*"[^"]+"' mobile-app/package.json 2>/dev/null | head -1 | sed -E 's/.*"([^"]+)"$/\1/')
  echo "  mobile-app/  Expo ${SDK_VER:-?}"
fi

# Env staleness check for spa (only stack with a Key Vault pull right now)
ENV_FILE="spa/apps/frontend/.env"
if [ -f "$ENV_FILE" ]; then
  ENV_MOD=$(stat -f %m "$ENV_FILE" 2>/dev/null || echo 0)
  NOW=$(date +%s)
  ENV_AGE=$(( (NOW - ENV_MOD) / 3600 ))
  if [ "$ENV_AGE" -gt 24 ]; then
    echo ""
    echo "WARNING: spa/apps/frontend/.env is ${ENV_AGE}h old. Consider: pnpm env:pull"
  fi
fi
