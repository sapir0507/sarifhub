# SarifHub backend

ASP.NET Core (.NET 10) API over PostgreSQL 17. Setup, architecture and examples are in the
[root README](../README.md); design documents are under [`docs/`](../docs).

| Command (from this folder) | What it does |
|---|---|
| `dotnet build` | Build the solution (warnings are errors) |
| `dotnet format --verify-no-changes` | Check formatting and code style |
| `dotnet tool restore` | Install the pinned `dotnet-ef` local tool |
| `dotnet ef database update --project src/SarifHub.Infrastructure --startup-project src/SarifHub.Api` | Apply migrations |
| `dotnet ef migrations add <Name> --project src/SarifHub.Infrastructure --startup-project src/SarifHub.Api --output-dir Persistence/Migrations` | Add a migration |
| `dotnet run --project src/SarifHub.Api -- seed` | Load the demo data into an empty database |
| `dotnet run --project src/SarifHub.Api` | Run the API on http://localhost:5080 (Scalar at `/scalar`) |

## Where things live

| Folder | Contents |
|---|---|
| `src/SarifHub.Domain` | Entities (`Project`, `Scan`, `Finding`, …), value objects, `TriagePolicy`, `QualityGateEvaluator` |
| `src/SarifHub.Application` | One class per use case (`ListProjects`, `SearchFindings`, …), read-model ports, DTOs, `FindingsQueryParser` |
| `src/SarifHub.Infrastructure/Persistence` | `SarifHubDbContext`, one configuration class per table, migrations |
| `src/SarifHub.Infrastructure/ReadModels` | Dapper SQL for projects, dashboard, scans, findings grid, trends; EF Core for finding details and tools |
| `src/SarifHub.Infrastructure/Seeding` | Development data, built through the domain model |
| `src/SarifHub.Api/Controllers` | One thin controller per API area |
