#!/bin/bash
# Detect tech stacks in a project root and print suggested manifest entries.
#
# Usage: detect-stacks.sh [path]  (default: current dir)
# Output: lines like "<stack-tag>: <evidence>"

set -euo pipefail

TARGET="${1:-.}"

if [[ ! -d "$TARGET" ]]; then
  echo "ERROR: $TARGET is not a directory" >&2
  exit 1
fi

cd "$TARGET"

# Helpers
detect() {
  local tag="$1"; shift
  local evidence="$1"; shift
  echo "$tag: $evidence"
}

found_any=false

# dotnet — .slnx, .csproj, Directory.Build.props
if compgen -G "**/*.slnx" >/dev/null 2>&1 || compgen -G "**/*.csproj" >/dev/null 2>&1 || compgen -G "**/*.sln" >/dev/null 2>&1; then
  detect "dotnet" "$(ls **/*.slnx **/*.sln **/*.csproj 2>/dev/null | head -3 | tr '\n' ' ')"
  found_any=true
fi

# nextjs — next.config.* or "next" in package.json deps
if compgen -G "**/next.config.*" >/dev/null 2>&1 || grep -rsq '"next":' --include="package.json" .; then
  detect "nextjs" "next.config or 'next' dep"
  detect "react" "(implied by nextjs)"
  found_any=true
fi

# react (standalone)
if grep -rsq '"react":' --include="package.json" . && ! grep -rsq '"next":' --include="package.json" .; then
  detect "react" "'react' dep without next"
  found_any=true
fi

# tailwind
if grep -rsq '"tailwindcss":' --include="package.json" . || compgen -G "**/tailwind.config.*" >/dev/null 2>&1; then
  detect "tailwind" "tailwindcss dep or config"
  found_any=true
fi

# turborepo
if [[ -f "turbo.json" ]] || compgen -G "**/turbo.json" >/dev/null 2>&1; then
  detect "turborepo" "turbo.json present"
  found_any=true
fi

# expo / react-native
if grep -rsq '"expo":' --include="package.json" .; then
  detect "expo" "'expo' dep"
  detect "react-native" "(implied by expo)"
  found_any=true
fi
if grep -rsq '"react-native":' --include="package.json" . && ! grep -rsq '"expo":' --include="package.json" .; then
  detect "react-native" "'react-native' dep without expo"
  found_any=true
fi

# azure devops pipelines
if [[ -d ".azuredevops" ]] || compgen -G "azure-pipelines*.yml" >/dev/null 2>&1; then
  detect "azure" "Azure DevOps pipelines / .azuredevops dir"
  found_any=true
fi

# auth0
if grep -rsq '"@auth0/nextjs-auth0":' --include="package.json" . || grep -rsq '"@auth0/auth0-react":' --include="package.json" .; then
  detect "auth0" "@auth0/* dep"
  found_any=true
fi

# Always suggest generic
detect "generic" "(always applicable)"

if [[ "$found_any" != true ]]; then
  echo "WARNING: no recognised stacks detected. The toolbox will land with generic-* only." >&2
fi
