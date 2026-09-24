# SarifHub

**Security findings intelligence for SARIF.** SarifHub ingests SARIF files from static analysis tools,
tracks each finding across scans with a stable fingerprint, tells you what is new, fixed and reintroduced,
supports triage with audited decisions, and turns it all into a quality gate your CI can call.

> **Status: Phase 1 of 10 — product and UI design.** The frontend runs against an in-memory mock that
> follows the planned API contract. The ASP.NET Core backend starts in Phase 3.

## What works today

- Projects list, project dashboard, findings grid, finding details, scans, scan diff and trends
- Server-style pagination, sorting, filtering and search, with state kept in the URL
- Triage with role-based rules (false positive and accepted risk require the Security lead role)
- English and Hebrew, with full right-to-left layout

## Run the frontend

Requirements: Node.js 20.19+ or 22.12+.

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173 and sign in as `demo@sarifhub.local` with any password (demo build only).
Use **View as** in the top bar to switch your role on the current project and see how permissions change.

## Documentation

- [Product and UI plan](docs/product/ui-plan.md) — vocabulary, screens, API contract, design decisions

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Product and dashboard design, frontend on mock data | Done |
| 2 | Architecture and database design | Next |
| 3 | Backend foundation (ASP.NET Core, PostgreSQL, EF Core) | |
| 4 | SARIF parser and generated test fixtures | |
| 5 | Fingerprinting and scan diff engine | |
| 6 | Authentication and authorization (JWT, API keys) | |
| 7 | Frontend on the real API | |
| 8 | Unit and integration tests | |
| 9 | Docker Compose and GitHub Actions | |
| 10 | Documentation, screenshots, release | |

## License

[MIT](LICENSE)
