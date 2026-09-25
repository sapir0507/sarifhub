# SarifHub — Product & UI Plan (Phase 1)

This document is the planning artifact for Phase 1. It defines the vocabulary, the screens,
the API contract the UI depends on, and the decisions taken before any backend code exists.
The frontend in `/frontend` implements this plan against an in-memory mock.

## 1. Users and roles

| Role | Typical person | Can do |
|---|---|---|
| Viewer | Engineering manager, auditor | Read dashboards, findings, scans, trends |
| Developer | Engineer on the repository | Viewer + confirm a finding + reset it to *Needs triage* |
| SecurityLead | AppSec engineer | Developer + mark **false positive** + **accept risk** with expiry |
| Admin | Project owner | SecurityLead + manage project, members, API keys, gate policy |

Roles are **per project** (`ProjectMember`), not global. A person can be Admin on one project and Viewer on another.

**Decision — false positive needs SecurityLead.** Marking a finding as a false positive removes it from
the quality gate, which is the same effect as accepting a risk. If developers could do it, a failing CI gate
could be silenced by the people it is meant to block. The matrix lives in `frontend/src/domain/permissions.ts`
and will be mirrored by ASP.NET Core authorization policies in Phase 6; the UI only hides what the API forbids.

## 2. Vocabulary

Two independent dimensions describe a finding. Keeping them separate is the core modelling decision.

**Lifecycle** — computed by the scan diff engine, never edited by a person.

| Status | Meaning (relative to the latest scan) |
|---|---|
| New | First time this fingerprint has ever been seen |
| Existing | Present now and in the previous scan |
| Reopened | Present now, absent in the previous scan, seen at some earlier point |
| Resolved | Absent from the latest scan |

**Triage** — a human decision, stored as an append-only history.

| Status | Meaning | Effect on counts and gate |
|---|---|---|
| Needs triage | No decision yet | Counted |
| Confirmed | Real finding | Counted |
| False positive | The tool is wrong | Excluded |
| Accepted risk | Real, accepted until a date | Excluded until the expiry date, then counted again |

**Active finding** = lifecycle is not Resolved **and** it is not a false positive **and** it is not an unexpired accepted risk.
The dashboard severity counts and the quality gate both use this definition.

**Quality gate** — evaluated per scan against a per-project policy
(`maxCritical`, `maxHigh`, `maxMedium`; `null` = not enforced). The scan fails if any active count exceeds its threshold.

## 3. Information architecture

```
/login
/projects                                   Projects list
/projects/:projectId                        Project dashboard
/projects/:projectId/findings               Findings grid (query string = filters, sort, page)
/projects/:projectId/findings/:findingId    Finding details + triage
/projects/:projectId/scans                  Scan history
/projects/:projectId/scans/:scanNumber      Scan details (diff vs previous scan)
/projects/:projectId/trends                 Trend charts
```

"Findings" in navigation opens **open** findings (`?status=New,Reopened,Existing`). Resolved findings are one filter away.

## 4. Screens

### 4.1 Login
- Email + password. Error message comes from the API's Problem Details `title`.
- Demo build: any password works for `demo@sarifhub.local`. Real credentials arrive with the seed in Phase 12.

### 4.2 Projects
| Column | Source |
|---|---|
| Project (+ your role) | `name`, `myRole` |
| Repository | `repositoryUrl` |
| Last scan | `lastScan.number`, `lastScan.uploadedAt` |
| Active findings | `activeBySeverity` (four compact counts) |
| Quality gate | `lastScan.gate.result` |

Empty state per row: "No scans uploaded yet".

### 4.3 Project dashboard

```
┌──────────────────────────────────────────────┬────────────────────────┐
│ Active findings                          34  │ Quality gate   FAILED  │
│ [ 5 Critical ][ 14 High      ][ 10 Med ][5L] │ Critical    5 / 0      │
│  (segment width ∝ count, click = filter)     │ High       14 / 10     │
├──────────────────────────────────────────────┴────────────────────────┤
│ Latest scan #18: New 3 | Resolved 6 | Reopened 1 ║ Needs triage 23 |   │
│                                                   ║ Accepted 3 | FP 3 │
├──────────────────────────────────────────────┬────────────────────────┤
│ Active findings by severity (stacked area)   │ Recent scans (6)       │
│                                              │ #18  +3/−6    Failed   │
└──────────────────────────────────────────────┴────────────────────────┘
```

- **Signature element: the severity ribbon.** One bar whose segments are proportional to the counts answers
  "how bad, and where is the weight" at a glance; four equal cards cannot show proportion. Each segment keeps
  a minimum width so 2 criticals stay visible next to 80 lows.
- The stat strip is grouped (this scan vs. triage) instead of six identical cards, because the grouping carries meaning.
- Every number is a link to the filtered findings or scan view that produced it.
- Accepted risks show "N expire within 14 days".
- Empty state (no scans): explains how to upload SARIF from CI.

### 4.4 Findings grid
MUI X DataGrid (MIT) in **server mode** for pagination and sorting; filtering uses a custom toolbar
because the free DataGrid supports only one column filter at a time.

| Column | Sortable | Notes |
|---|---|---|
| Severity | yes | Colored marker + text (never color alone) |
| Status (lifecycle) | yes | |
| Rule | yes (by id) | Name + rule id; name is a real link for keyboard users |
| File | yes | File name, folder underneath; full path in tooltip |
| Line | yes | |
| Tool | yes | |
| Triage | yes | Accepted risk shows expiry tooltip / "expired" |
| First seen / Last seen | yes | |

Toolbar: free-text search (debounced 300 ms) + multi-selects for severity, status, triage, tool + "Clear filters".
**All state is in the URL** so a filtered view can be bookmarked, shared in a ticket, and Back works.

### 4.5 Finding details
- Header: severity, lifecycle, triage chips; rule name; rule id (tool); **Triage** button (disabled with an explanation for roles that cannot triage).
- Message + rule description.
- Code snippet with the reported line highlighted (always LTR, horizontally scrollable).
- **Occurrences**: one row per scan the finding appeared in, with the line number at that time and the lifecycle
  computed in that scan. This is where the "same finding, moved line" behaviour of fingerprinting is visible.
- Details: tool, first/last seen, CWE, fingerprint (truncated, full value in tooltip).
- Triage history, newest first: decision, reason, expiry, who, when.

### 4.6 Triage dialog
- Options: Confirmed, False positive, Accepted risk, Reset to needs triage. Options the role cannot use are disabled with "Requires the Security lead role".
- Reason: required (≥ 10 characters) for everything except Confirmed; max 1,000 characters.
- Accepted risk: expiry date required, between tomorrow and 12 months ahead.
- The same rules are validated again by the API; the UI is not a security boundary.
- On success: dialog closes, snackbar "Triage decision saved", finding + lists + dashboard counts refresh.

### 4.7 Scans list
Scan number and time, commit, uploaded by (user or CI key name), total / new / resolved / reopened, gate result.

### 4.8 Scan details
```
Scan #42 — compared with scan #41
┌───────┬───────┬──────────┬──────────┬──────────┐   ┌──────────────┐
│ 127   │ 8     │ 13       │ 2        │ 104      │   │ Quality gate │
│ Total │ New   │ Resolved │ Reopened │ Existing │   │   Failed     │
└───────┴───────┴──────────┴──────────┴──────────┘   └──────────────┘
Commit · Branch · Uploaded by · Uploaded at · Tools
Findings in this scan  [same grid, scoped to scan 42, lifecycle as computed in scan 42]
```
Each diff number is a toggle that filters the grid below. Resolved findings in this scan are included in the
scoped grid (they disappeared in this scan), so "13 resolved" is inspectable.

### 4.9 Trends
- Active findings by severity (stacked area)
- New / resolved / reopened per scan (grouped bars)
- Findings by tool (lines)

The x-axis is the scan number, not calendar time: scans are irregular, and per-scan is what the diff engine produces.

## 5. API contract used by the UI

Types: `frontend/src/api/types.ts`. Interface: `frontend/src/api/client.ts`. Phase 3 implements these endpoints
with the same shapes; errors use RFC 9457 Problem Details.

| Method | Path | Notes |
|---|---|---|
| POST | `/api/auth/login` | Returns the user; token handling decided in Phase 6 |
| GET | `/api/projects` | Includes `myRole`, last scan, active counts |
| GET | `/api/projects/{id}/dashboard` | One call per dashboard render |
| GET | `/api/projects/{id}/findings` | `page, pageSize, sort, dir, severity, status, triage, tool, q, scan` |
| GET | `/api/projects/{id}/findings/{findingId}` | Detail + occurrences + triage history |
| POST | `/api/projects/{id}/findings/{findingId}/triage` | `{ status, reason, expiresAt }` → 400 / 403 / 200 |
| GET | `/api/projects/{id}/scans` | Newest first |
| GET | `/api/projects/{id}/scans/{number}` | Diff counts, gate, severity breakdown |
| GET | `/api/projects/{id}/trends` | One point per scan |
| GET | `/api/projects/{id}/tools` | Filter options |

CI endpoints (SARIF upload with API key, gate check) do not appear in the UI and are designed in Phases 2–3.

## 6. Design system

| Token | Value | Use |
|---|---|---|
| Ink | `#1C2330` | Text |
| Canvas / Surface / Line | `#F3F5F7` / `#FFFFFF` / `#DCE0E7` | Background, panels, hairlines (no drop shadows) |
| Petrol / Petrol deep | `#0E5A6B` / `#0A3440` | Primary actions, sidebar |
| Severity | Critical `#A4161A`, High `#C9510C`, Medium `#A87B06`, Low `#4F6D9A` | Reserved for severity only |
| Lifecycle | New `#5B3CC4`, Reopened `#9C2F6B`, Resolved `#2E7D4F` | Distinct from severity hues |

- Type: **IBM Plex Sans + IBM Plex Sans Hebrew** (one family covers both languages with matching metrics),
  **IBM Plex Mono** for code, paths and hashes only. Self-hosted through Fontsource: no third-party requests,
  which keeps a strict Content-Security-Policy possible.
- Severity is never communicated by color alone: every marker has text.
- Motion: none on load; only feedback transitions (hover, dialog).

## 7. Language and direction

- English is the default UI language (the repository is read by international reviewers); Hebrew is a full toggle.
- RTL is handled once, at the styling layer: a second Emotion cache runs `stylis-plugin-rtl`, which flips every MUI
  component and `sx` style. Code, paths, commit hashes and numeric ratios stay LTR (`<bdi dir="ltr">`); inside those
  islands only logical CSS properties are used, because the plugin would otherwise flip physical ones.
- Charts keep a left-to-right time axis in both languages.
- i18n is a typed dictionary (`frontend/src/app/i18n.ts`); the Hebrew dictionary must define every English key or
  the build fails. A library was not justified for two languages and ~150 strings.

## 8. Frontend architecture decisions

| Decision | Why |
|---|---|
| All data access behind one `SarifHubApi` interface | Phase 1 ships a mock; switching to HTTP is one file. The interface doubles as the API contract. |
| TanStack Query for server state | Server-side paging, and triage must invalidate the finding, lists and counts together. Query keys make that explicit. |
| Filters, sort and page in the URL | Shareable, bookmarkable, Back-button friendly. |
| Deterministic mock (seeded PRNG) | Stable screenshots; the mock simulates logical findings across scans, line drift, regressions and expiring risk acceptances. |
| Hash routing only in the static demo build | `vite build --mode demo`; normal builds use browser history routing. |

## 9. Deferred to later phases

- Token storage and refresh (Phase 6). The mock keeps only the display user in `sessionStorage`, never a token.
- Code splitting per route (Phase 7): the bundle is ~1.6 MB before gzip today.
- Project settings screen: members, API keys, gate policy (after Phase 6).
- Real SARIF-derived data replaces the mock rule pack (Phase 4).
