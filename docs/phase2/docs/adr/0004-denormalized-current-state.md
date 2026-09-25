# 0004 — Current state is denormalized onto `findings`

**Status:** Accepted (Phase 2)

**Context.** Occurrences and triage decisions are histories. Every list, count and filter needs the *latest* value.

**Decision.** `findings` stores current lifecycle, triage status, acceptance expiry, last location and message.
History tables remain the record. Only two handlers write these columns (scan ingestion, triage), each in one transaction.
Triage uses optimistic concurrency (PostgreSQL `xmin` as EF Core concurrency token, exposed as an `ETag`).

**Consequences.** The default grid page is an index scan that stops after 25 rows (0.2 ms at 20,000 findings), and the
diff engine gets its whole input from one query. The cost is dual writes; integration tests assert that the finding row and
the latest history row always agree.
