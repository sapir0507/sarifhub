# 0002 — Five projects with inward dependencies

**Status:** Accepted (Phase 2)

**Context.** The core of SarifHub — parsing, fingerprinting, diffing, gate evaluation, triage rules — is logic that must
be tested exhaustively without a database. HTTP and persistence are comparatively thin.

**Decision.** `Api` → `Infrastructure` → `Application` → `Sarif` → `Domain`, where an arrow means "depends on" (Api also references Application directly as the composition root).
Domain and Sarif have no framework dependencies. SARIF parsing is its own project so the external format does not leak into the domain model.

**Alternatives.** A single project with folders (simplest, but nothing stops the diff engine from reaching for `DbContext`);
vertical slices (good for large feature counts, less clear for one dominant pipeline).

**Consequences.** More project files, and interfaces for repositories and the clock. In exchange, the unit test project
references only Domain, Sarif and Application, and runs in milliseconds.
