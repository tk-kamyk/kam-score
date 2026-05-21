---
name: nextjs-frontend-standards
description: Apply when writing, reviewing, or refactoring code in spa/ — the Continia.Card admin portal. Covers stack, layout, design tokens, UI primitives, and frontend anti-patterns. The Auth flow detail lives in vendor-auth0-bff.
metadata:
  stack: [nextjs, react, tailwind]
  requires: [vendor-auth0-bff]
---

# Frontend standards (spa/)

## Layout

pnpm workspaces + Turborepo at `spa/`; mirrors `/insp` 1:1 so a later migration is a directory merge. Apps live in `spa/apps/<name>/`; shared packages in `spa/packages/`; tooling in `spa/tools/config-*/`.

## Stack

- Next.js 16 App Router, React 19, TypeScript 5.9.
- Tailwind 4 with inline `@theme` and design tokens from `@repo/config-tailwind`.
- `@auth0/nextjs-auth0` v4 + BFF proxy at `spa/apps/frontend/app/api/proxy/[...path]/route.ts` (see `vendor-auth0-bff`).
- TanStack Query 5.
- Orval-generated API client in `@repo/api`; mutator routes everything through `/api/proxy`.
- MSW v2 — gated by `NEXT_PUBLIC_API_MOCKING=enabled`, defaults to `disabled`.
- React Hook Form + Zod for forms.
- zustand for UI state.
- sonner for toasts.
- `@sentry/nextjs` for error capture and tracing (see `nextjs-sentry`).

## Design tokens (PascalCase)

Use the named tokens, not bare colours:

- `bg-BackgroundNeutralBaseDefault`
- `text-ContentNeutralStrong`
- `border-BorderInputDefault`
- etc.

Tokens live in `@repo/config-tailwind/tokens.css`. Standard Tailwind utilities (`mt-4`, `px-2.5`, `flex`, `gap-2`) remain lowercase — only the **token names** are PascalCase.

## UI primitives (`@repo/ui`)

Follow shadcn/Radix conventions:

- `forwardRef` for every primitive that accepts refs.
- `displayName` set explicitly for DevTools.
- `data-slot` attribute on the root element of each primitive.
- CVA (`class-variance-authority`) for variants.
- `cn()` (from `@repo/utils`) for class merging — **never** string-concat or template literals for class names.

## Frontend ANTI-PATTERNS

- **Never call the .NET API directly from the browser.** Always through `/api/proxy/...` (the BFF). The browser doesn't have the bearer token; the BFF injects it server-side.
- **Never store tokens in zustand or localStorage.** The auth-store persists `{ user, isAuthenticated, returnUrl, tokenExpiry }` only. Token handling is in the BFF.
- **Never echo `IdentityNumber`** in toasts, confirm dialogs, history rows, or logs. See `docs/design/identity-verification.md` for full rules; in short: it's sensitive PII and not for display.
- **Never edit `spa/packages/api/src/generated/`.** Re-run `pnpm generate` after API contract changes.
- **Never bypass `customInstance`.** It owns auth, error normalisation, and content-type negotiation. Going around it breaks all three.
- **Never enable MSW in production.** `NEXT_PUBLIC_API_MOCKING` defaults to `disabled` for a reason — accidentally shipping MSW responses is a data leak.

## Commands (from `spa/`)

| Command | What it does |
|---|---|
| `pnpm install` | Install all workspace deps via pnpm + catalogs |
| `pnpm dev` | Turbo routes to `@continia-card/frontend`; HTTPS on `:3000` |
| `pnpm build` | Production build via Turborepo; fails on type errors |
| `pnpm check-types` | TypeScript-only validation (no emit) |
| `pnpm lint` | Lint via oxlint (per workspace config) |
| `pnpm test` | Vitest |
| `pnpm format` | Prettier |
| `pnpm generate` | Refresh Orval client (`@repo/api`); requires API on `:5001` or a committed `openapi/v1.json` snapshot |

## Related skills

- `vendor-auth0-bff` — full BFF auth flow detail.
- `nextjs-ui-implementation` — pixel-perfect UI workflow (Chrome MCP, Figma compare).
- `nextjs-sentry` — error capture patterns.
- `nextjs-env-var` — adding env vars end-to-end (Key Vault + Zod + .env.example).
- `nextjs-vercel-react-best-practices` — 45 performance rules.
- `nextjs-verify-chrome` — visual verification flow.
- `turborepo-conventions` — monorepo task config.
