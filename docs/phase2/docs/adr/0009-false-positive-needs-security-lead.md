# 0009 — Marking a false positive requires SecurityLead

**Status:** Accepted (Phase 1)

**Context.** A false positive is excluded from counts and from the quality gate — the same effect as accepting a risk.

**Decision.** Developers may confirm findings and reset them. False positive and accepted risk require SecurityLead or Admin.
The matrix is defined once in the domain (`TriagePolicy`) and mirrored in the UI only to hide unavailable actions.

**Consequences.** The people a failing gate blocks cannot silence it on their own. v2 option: developers *propose*
a false positive and a SecurityLead approves it.
