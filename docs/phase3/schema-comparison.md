# Migration vs. `schema.sql`

From Phase 3 the EF Core migrations in `backend/src/SarifHub.Infrastructure/Persistence/Migrations` are the source of
truth for the database. [`schema.sql`](../phase2/docs/architecture/schema.sql) remains the readable description of the
intended schema. This page records how the two were compared and every difference.

## Method

1. Create two empty PostgreSQL 17 databases.
2. Apply `schema.sql` to the first and `dotnet ef database update` to the second.
3. Run [`schema-compare.sql`](schema-compare.sql) against both and `diff` the output. The script lists every column
   (type, nullability, default, generation expression), every CHECK, foreign key and primary key definition, every
   index definition (without its name) and the installed extensions.

## Result

Identical: all 13 shared tables, the type and default of every shared column, all 31 CHECK constraints (including the regular
expressions, `num_nonnulls`, the accepted-risk ⇔ expiry rules and the triage reason rule), every foreign key with its
`ON DELETE` behaviour, every unique constraint, the partial indexes (`ix_findings_open_severity`,
`ix_findings_risk_expiry`), the descending indexes, the trigram GIN indexes and the `pg_trgm` extension.

Differences:

| Difference | Why |
|---|---|
| `users` has no `password_hash`, `security_stamp`, `lockout_end`, `access_failed_count`; no `refresh_tokens` table | Planned. These belong to ASP.NET Core Identity and token handling, added by the Phase 6 migration. Nothing in Phase 3 stores credentials. |
| `findings.severity_rank` is `NOT NULL` | Stricter than `schema.sql`: it is generated from `severity`, which is `NOT NULL`, so it can never be null. |
| Seven additional indexes on foreign key columns (`api_keys.created_by_id`, `projects.created_by_id`, `scans.uploaded_by_user_id`, `scans.uploaded_by_key_id`, `findings.first_seen_scan_id`, `findings.last_seen_scan_id`, `triage_decisions.decided_by_id`) | EF Core indexes every foreign key by convention. Kept: they make deleting a user or a scan check referencing rows through an index instead of a table scan. The write cost is negligible at this volume. |
| The severity CHECK lists values `Low … Critical` instead of `Critical … Low` | Same constraint. The allowed values are generated from the C# enum, so the database and the domain cannot drift. |
| Constraint and index names | EF Core and PostgreSQL name unnamed constraints differently (`ck_…`, `ix_…`, `pk_…`). Definitions are what the comparison checks. |

## PostgreSQL-specific features used on purpose

| Feature | Where | Why |
|---|---|---|
| `pg_trgm` + GIN indexes | `findings.file_path`, `findings.message` | "Contains" search (`ILIKE '%…%'`) cannot use a B-tree index |
| Partial indexes | open findings by severity, accepted risks by expiry | Index only the rows the hot queries read |
| Stored generated column | `findings.severity_rank` | Lets the default grid order walk an index instead of sorting |
| `xmin` as row version | `projects`, `findings` | Optimistic concurrency without an extra column; used by triage (`If-Match`) in Phase 6 |
| `text[]` | `rules.cwe`, `api_keys.scopes` | Small value lists, validated with array CHECKs (`<@`, `cardinality`) |
| `jsonb` | `scans.gate_evaluation`, `finding_occurrences.partial_fingerprints`, `audit_events.data` | Snapshots whose shape is read whole, never queried by field |
| `inet`, identity column | `audit_events` | Typed client address; append-only log with a database sequence |
