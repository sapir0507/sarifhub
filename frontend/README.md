# SarifHub frontend

React 19, TypeScript (strict), MUI with MUI X DataGrid, Recharts, TanStack Query, React Router.

| Command | What it does |
|---|---|
| `npm run dev` | Dev server on http://localhost:5173 |
| `npm run typecheck` | TypeScript project check |
| `npm run lint` | Oxlint |
| `npm run build` | Type-check and production build |
| `npm run build:demo` | Static demo build with hash routing (`.env.demo`) |

## Structure

```
src/
  api/          API contract (types.ts), client interface, query hooks, mock implementation
  app/          Theme and design tokens, i18n, locale/RTL provider, router, shared hooks
  auth/         Session provider and route guard
  components/   Shared UI: severity ribbon, quality gate panel, findings explorer, triage dialog, charts
  domain/       Permission matrix and link helpers shared by pages
  pages/        One component per route
```

All data goes through `SarifHubApi` (`src/api/client.ts`). In Phase 1 it is backed by a deterministic mock
in `src/api/mock`; Phase 7 replaces it with an HTTP client for the ASP.NET Core API.
