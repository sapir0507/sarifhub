# 0008 — The quality gate result is an immutable snapshot on the scan

**Status:** Accepted (Phase 2). Changes Phase 1 mock behaviour (live evaluation) in Phase 7.

**Decision.** Evaluate the gate when a scan completes and store the result together with the policy and counts used.
Later triage or policy changes do not rewrite past results. Current posture is shown separately (dashboard active counts).

**Consequences.** CI history is reproducible and auditable. A user who accepts a risk after a failed build re-runs CI
(or re-uploads) to get a new passing scan — the same model GitHub checks use.
