# Architecture Decision Records

Short records of decisions that shaped SarifHub: the context, the choice, and what it costs.

| # | Decision | Phase |
|---|---|---|
| [0001](0001-dotnet-10.md) | Target .NET 10 (LTS) | 2 |
| [0002](0002-layered-solution.md) | Five projects with inward dependencies | 2 |
| [0003](0003-efcore-writes-dapper-reads.md) | EF Core for writes and migrations, Dapper for read models | 2 |
| [0004](0004-denormalized-current-state.md) | Current state denormalized onto `findings` | 2 |
| [0005](0005-identity-and-jwt.md) | Identity + self-issued JWT with rotating refresh cookie | 2 |
| [0006](0006-synchronous-ingestion.md) | Synchronous ingestion, serialized per project | 2 |
| [0007](0007-api-key-hashing.md) | API keys: public prefix + SHA-256 | 2 |
| [0008](0008-gate-snapshot.md) | Quality gate result is an immutable snapshot | 2 |
| [0009](0009-false-positive-needs-security-lead.md) | False positive requires SecurityLead | 1 |
