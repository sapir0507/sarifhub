# SarifHub — API Contract (Phase 2)

Base path `/api`. JSON uses camelCase. Timestamps are ISO 8601 UTC. IDs are UUIDs, except scan numbers,
which are per-project integers because people and CI logs refer to "scan #42".
The OpenAPI document generated in Phase 3 is the executable version of this file.

## 1. Conventions

**Errors** — RFC 9457 Problem Details, `Content-Type: application/problem+json`:

```json
{ "type": "https://sarifhub.dev/problems/validation", "title": "The request is invalid.", "status": 400,
  "errors": { "reason": ["Add a reason of at least 10 characters."] }, "traceId": "00-4bf9…-01" }
```

| Status | When |
|---|---|
| 400 | Validation failed (body, query, SARIF content) |
| 401 | Missing or invalid token / API key |
| 403 | Authenticated, but the project role or key scope does not allow it |
| 404 | Not found **or not a member of the project** — non-members cannot probe which projects exist |
| 409 | Optimistic concurrency conflict (someone triaged the finding first) |
| 428 | A required `If-Match` header is missing |
| 413 | Upload larger than the limit |
| 429 | Rate limit (login, upload) |

**Paging** — `page` (0-based), `pageSize` (25, 50 or 100). Responses: `{ items, totalCount, page, pageSize }`.
**Sorting** — `sort` from a whitelist per endpoint, `dir` = `asc` | `desc`. Unknown values → 400; never interpolated into SQL.

## 2. Authorization matrix

| Capability | Viewer | Developer | SecurityLead | Admin | CI key |
|---|:-:|:-:|:-:|:-:|:-:|
| Read project, dashboard, findings, scans, trends | ✓ | ✓ | ✓ | ✓ | |
| Triage: Confirmed, reset to Untriaged | | ✓ | ✓ | ✓ | |
| Triage: False positive, Accepted risk | | | ✓ | ✓ | |
| Upload scan from the UI | | ✓ | ✓ | ✓ | |
| Manage members, gate policy, API keys, project settings | | | | ✓ | |
| Upload scan (`scans:write`) | | | | | ✓ |
| Read gate result (`gate:read`) | | | | | ✓ |
| Read project audit log | | | ✓ | ✓ | |

Any authenticated user can create a project and becomes its Admin.

## 3. Endpoints

### Authentication (anonymous, rate-limited)
| Method | Path | Body → Response |
|---|---|---|
| POST | `/auth/login` | `{ email, password }` → `{ accessToken, expiresAt, user }` + refresh cookie |
| POST | `/auth/refresh` | (refresh cookie) → `{ accessToken, expiresAt }`, rotates the cookie |
| POST | `/auth/logout` | revokes the refresh token, clears the cookie → 204 |
| GET | `/auth/me` | → `{ id, email, displayName }` |

Registration is not public in v1: users are created by the seed (demo) or by an Admin invite (v2).

### Projects
| Method | Path | Min. role | Notes |
|---|---|---|---|
| GET | `/projects` | member | Only projects the caller belongs to, with `myRole`, last scan, active counts |
| POST | `/projects` | any user | `{ key, name, repositoryUrl?, defaultBranch? }` → 201 |
| GET | `/projects/{projectId}` | Viewer | |
| PATCH | `/projects/{projectId}` | Admin | name, repository, default branch |
| PUT | `/projects/{projectId}/gate-policy` | Admin | `{ maxCritical, maxHigh, maxMedium }`, nulls allowed |
| GET | `/projects/{projectId}/members` | Viewer | |
| PUT | `/projects/{projectId}/members/{userId}` | Admin | `{ role }`; the last Admin cannot be demoted or removed |
| DELETE | `/projects/{projectId}/members/{userId}` | Admin | |

### API keys (Admin)
| Method | Path | Notes |
|---|---|---|
| GET | `/projects/{projectId}/api-keys` | Name, prefix, scopes, created, last used, expiry, revoked. Never the key. |
| POST | `/projects/{projectId}/api-keys` | `{ name, scopes, expiresAt? }` → 201 `{ id, key: "shk_ab12cd34_…", … }` — **the only time the key is returned** |
| DELETE | `/projects/{projectId}/api-keys/{keyId}` | Revokes (sets `revokedAt`); the row stays for the audit trail |

### Dashboard, scans, trends (Viewer)
| Method | Path | Notes |
|---|---|---|
| GET | `/projects/{projectId}/dashboard` | Shape: `ProjectDashboard` in `frontend/src/api/types.ts` |
| GET | `/projects/{projectId}/scans` | Paged, newest first |
| GET | `/projects/{projectId}/scans/{number}` | Diff counts, severity counts, tools, stored gate evaluation |
| POST | `/projects/{projectId}/scans` | Developer+. Same pipeline as CI upload, for manual uploads from the UI |
| GET | `/projects/{projectId}/trends` | One point per scan, from scan snapshots |
| GET | `/projects/{projectId}/tools` | Distinct tool names, for filters |

### Findings
| Method | Path | Min. role | Notes |
|---|---|---|---|
| GET | `/projects/{projectId}/findings` | Viewer | `severity, status, triage, tool` (comma lists), `q`, `scan`, `sort`, `dir`, paging |
| GET | `/projects/{projectId}/findings/{findingId}` | Viewer | Includes occurrences, resolutions and triage history. Returns an `ETag`. |
| POST | `/projects/{projectId}/findings/{findingId}/triage` | Developer | `If-Match: <etag>` required; see rules below |

Triage request: `{ "status": "AcceptedRisk", "reason": "…", "expiresAt": "2026-12-31T00:00:00Z" }`

| Rule | Error |
|---|---|
| Role may not use this status | 403 |
| `reason` < 10 chars (except Confirmed) or > 1,000 | 400 |
| AcceptedRisk without `expiresAt`, in the past, or > 12 months ahead | 400 |
| `expiresAt` sent for another status | 400 |
| `If-Match` missing | 428 |
| `If-Match` does not match the current version | 409 |

Sort whitelist: `severity, lifecycle, triage, tool, ruleId, filePath, line, firstSeenAt, lastSeenAt`.

### Audit (SecurityLead)
| Method | Path | Notes |
|---|---|---|
| GET | `/projects/{projectId}/audit` | Paged, newest first, filter by `action` |

## 4. CI endpoints (API key)

Authentication: `Authorization: Bearer shk_<prefix>_<secret>`. The key is bound to one project; the `{projectKey}`
in the URL must match it (mismatch → 404, same as an unknown project).

### Upload a scan — `scans:write`

```
POST /api/ci/projects/{projectKey}/scans
Content-Type: multipart/form-data
  file    = results.sarif          (required, ≤ 50 MB, SARIF 2.1.0)
  branch  = main                   (required)
  commit  = 9fceb02d0ae598e95dc970b74767f19372d61af8   (optional, 7–64 hex)
```

`201 Created`, `Location: /api/projects/{projectId}/scans/19`

```json
{
  "scanNumber": 19,
  "counts": { "total": 41, "new": 3, "existing": 37, "reopened": 1, "resolved": 6 },
  "gate": {
    "result": "Failed",
    "reasons": ["5 critical findings (max 0)", "14 high findings (max 10)"],
    "policy": { "maxCritical": 0, "maxHigh": 10, "maxMedium": null },
    "counts": { "critical": 5, "high": 14, "medium": 10, "low": 5 }
  },
  "url": "https://localhost:5173/projects/…/scans/19"
}
```

- Same file (same SHA-256, same commit) uploaded again → `200 OK` with the existing scan: CI retries are safe.
- Invalid SARIF → `400` with the first validation errors (JSON path + message), nothing stored.
- The upload succeeds even when the gate fails: the scan is data; failing the build is the caller's decision.

### Read a gate result — `gate:read`

```
GET /api/ci/projects/{projectKey}/scans/{number}/gate
GET /api/ci/projects/{projectKey}/scans/latest/gate?branch=main
```

Returns the `gate` object above. A CI step fails the build when `result` is `Failed`:

```bash
result=$(curl -sf -H "Authorization: Bearer $SARIFHUB_API_KEY" \
  "$SARIFHUB_URL/api/ci/projects/payments-api/scans/latest/gate?branch=main" | jq -r .result)
[ "$result" = "Passed" ] || { echo "SarifHub quality gate failed"; exit 1; }
```

## 5. Limits

| Limit | Value | Where enforced |
|---|---|---|
| Upload size | 50 MB | Kestrel request body limit + endpoint metadata |
| Results per file | 25,000 | Normalizer (400 above) |
| JSON depth | 64 | `JsonReaderOptions.MaxDepth` |
| Login attempts | 5 per minute per IP; lockout after 5 failures per account | Rate limiter + Identity lockout |
| Uploads | 30 per minute per API key | Rate limiter |
