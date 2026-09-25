# 0003 — EF Core for writes and migrations, Dapper for read models

**Status:** Accepted (Phase 2)

**Context.** Writes are aggregate-shaped (a scan with its occurrences, a triage decision updating a finding) and need
migrations and optimistic concurrency. Reads are report-shaped: a dashboard computes seven counts in one pass,
trends read snapshot rows, the grid needs a whitelisted dynamic `ORDER BY`.

**Decision.** EF Core owns the schema (migrations), all writes, and simple reads by id.
Dapper, sharing the same `NpgsqlDataSource`, runs hand-written SQL for the grid, dashboard, trends and scan views.
Read queries live in Infrastructure behind Application interfaces and are covered by integration tests against PostgreSQL.

**Consequences.** Two data-access styles to explain — which is the point: each is used where it is strongest, and the
measured query plans in `docs/architecture/data-model.md` are the SQL that actually runs.
Risk: a column rename must be applied to Dapper SQL by hand; integration tests catch this.
