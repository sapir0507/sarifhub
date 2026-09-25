# 0006 — Synchronous ingestion, serialized per project

**Status:** Accepted (Phase 2)

**Context.** CI wants the gate result in the upload response. The diff for scan N+1 depends on the state after scan N,
so two concurrent uploads for one project would compute from the same base and collide on the scan number.

**Decision.** Ingest inside the request. Parse and fingerprint before opening a transaction; inside it, take
`pg_advisory_xact_lock(hashtext(project_id))`, read state, diff, write, commit. Bound work with limits
(50 MB, 25,000 results). The handler is a plain application service.

**Alternatives.** Queue + background worker + polling (needed only when files or volumes grow beyond these limits).

**Consequences.** One HTTP call gives CI its answer; different projects ingest in parallel; the same project is serialized
by the database, not by in-process locks, so it stays correct with several API instances.
