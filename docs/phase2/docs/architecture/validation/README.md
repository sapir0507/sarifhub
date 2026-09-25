# Schema validation scripts

These scripts check the Phase 2 schema against a real PostgreSQL before any application code exists.

| Script | What it proves |
|---|---|
| `01-constraints.sql` | Invalid data is rejected by the database itself (bad keys, scopes, hashes, uploader, duplicates, triage rules) |
| `02-volume.sql` | Loads ~59,000 findings across 40 projects and ~457,000 occurrences for one large project |
| `03-explain.sql` | `EXPLAIN ANALYZE` for the grid, pager count, dashboard, search, scan view, diff input and finding history |

Run with Docker (no local PostgreSQL needed), from the repository root:

```bash
docker run -d --name sarifhub-schema -e POSTGRES_PASSWORD=local-only -p 55432:5432 postgres:17
docker exec -i sarifhub-schema psql -U postgres -c "CREATE DATABASE sarifhub"
docker exec -i sarifhub-schema psql -U postgres -d sarifhub -v ON_ERROR_STOP=1 < docs/architecture/schema.sql
docker exec -i sarifhub-schema psql -U postgres -d sarifhub < docs/architecture/validation/01-constraints.sql
docker exec -i sarifhub-schema psql -U postgres -d sarifhub < docs/architecture/validation/02-volume.sql
docker exec -i sarifhub-schema psql -U postgres -d sarifhub < docs/architecture/validation/03-explain.sql
docker rm -f sarifhub-schema
```

`01-constraints.sql` prints an `ERROR` line for every row that must be rejected; that is the expected output.
The password above is a throwaway for a local container, not a project secret.
