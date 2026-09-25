# Phase 3 verification

Checks run before Phase 3 was declared complete. Environment: Ubuntu 24.04, .NET SDK 10.0.112, PostgreSQL 17.10;
NuGet packages as pinned in `backend/Directory.Packages.props`.

## Build

| Check | Result |
|---|---|
| `dotnet build SarifHub.slnx -c Release` after deleting every `bin/` and `obj/` | 0 warnings, 0 errors (nullable, .NET analyzers in *Recommended* mode, code style — all as errors) |
| `dotnet format --verify-no-changes` | No changes |
| Project references | Domain → nothing; Sarif → Domain; Application → Domain, Sarif; Infrastructure → Application, Domain; Api → Application, Infrastructure. Application has no EF Core, Dapper, Npgsql or ASP.NET Core reference. |

## Database

| Check | Result |
|---|---|
| Empty PostgreSQL 17 database → `dotnet ef database update` | `InitialCreate` applied |
| Migration vs. `schema.sql` | Structurally identical except the planned differences in [schema-comparison.md](schema-comparison.md) |
| `dotnet run -- seed` on the migrated database | 5 users, 4 projects, 15 memberships, 3 API keys (hash only), 21 rules, 36 scans, 72 tool runs, 136 findings, 1,168 occurrences, 104 resolutions, 80 triage decisions |
| Seed consistency | For every scan: stored total/new/existing/reopened/resolved/severity counts = rows in `finding_occurrences` / `finding_resolutions`; tool result counts = occurrences per tool. For every finding: current lifecycle = its state in the latest scan; current triage = latest decision. 0 mismatches. |
| `seed` on a seeded database | Skipped, nothing changed |
| `seed` before migrating | Refused with a message pointing to `dotnet ef database update`, exit code 1 |
| Start over: `dotnet ef database drop`, `database update`, `seed` | Database recreated and reseeded with the same counts |

## API

An automated check (102 assertions) called every endpoint on the seeded database:

| Area | Verified |
|---|---|
| Shapes | Every response's property names equal the interface in `frontend/src/api/types.ts` (ProjectSummary, ProjectDashboard, ScanSummary, ScanDetail, TrendPoint, PagedResult, FindingListItem, FindingDetail, CodeSnippet, FindingOccurrence, TriageDecision, ToolInfo, GateEvaluation, SeverityCounts, QualityGatePolicy); enums serialize as names |
| Values | Dashboard active counts equal the projects list; totals equal the sum of severities; trend has one point per scan; recent scans newest first; scan-scoped findings = present + resolved in that scan; trend tool counts add up; empty project returns empty lists and `latestScan: null` |
| Findings | Default page 0 / size 25, severity descending; last page and beyond-the-end pages; paging with size 100 returns every finding exactly once; filters by severity, status, triage, tool, search and scan; every sort field; `%` in search is literal |
| Invalid input → 400 Problem Details with the parameter named | unknown sort field, a SQL fragment as sort field, page size 10, page −1, unknown direction, unknown enum values, numeric enum values, 201-character search, scan 0, non-numeric page, malformed GUIDs |
| Not found → 404 Problem Details | unknown project on every endpoint, unknown finding, a finding requested through another project, unknown scan number |
| Errors | `application/problem+json` with `traceId`; database stopped → `/health` 503 `Unhealthy`, API 503 "The database is unavailable." with no exception text; logs contain no connection string or password |
| Extras | `ETag` on finding details; OpenAPI document (8 paths) and Scalar UI; CORS allows `http://localhost:5173` (GET, exposes `ETag`) and no other origin |

## README walkthrough

From a fresh copy of the repository (no `bin/`, `obj/`, user secrets or database): steps 2–6 of *Running locally*
were run as written, command by command. This found and fixed one problem — `dotnet ef` needs a built solution in a
fresh clone, so step 3 now runs `dotnet build` first.

Not executed in this environment: the `docker run postgres:17` command (no container registry access; the same
role, database and password were created on a PostgreSQL 17.10 server instead) and the PowerShell password line.

## Repository

Scanned for secrets (passwords, connection strings, keys, tokens, private URLs, personal paths): none. The only
connection string is in user secrets outside the repository. Generated files are limited to the EF migration and
`packages.lock.json` files, which are meant to be committed.
