-- SarifHub reference schema (Phase 2 design artifact)
--
-- Purpose: review the data model and validate constraints and indexes against a real PostgreSQL
-- before any application code exists. From Phase 3 on, EF Core migrations are the source of truth;
-- this file is kept as readable documentation and is compared against the generated migration.
--
-- Conventions
--   * snake_case names (EF Core via EFCore.NamingConventions)
--   * uuid primary keys, generated in .NET as UUIDv7 (time-ordered: good index locality, no DB dependency)
--   * timestamptz everywhere, always written in UTC
--   * enums stored as text + CHECK: readable in SQL, and adding a value is a plain migration
--   * append-only tables (triage_decisions, audit_events) are never updated or deleted by the application

CREATE EXTENSION IF NOT EXISTS pg_trgm;  -- trigram indexes for "contains" search on file paths and messages

-- ---------------------------------------------------------------------------------------------
-- Identity (managed by ASP.NET Core Identity; only the columns SarifHub relies on are shown)
-- ---------------------------------------------------------------------------------------------
CREATE TABLE users (
    id                    uuid        PRIMARY KEY,
    email                 text        NOT NULL,
    normalized_email      text        NOT NULL UNIQUE,
    display_name          text        NOT NULL CHECK (length(display_name) BETWEEN 1 AND 100),
    password_hash         text        NOT NULL,           -- Identity PBKDF2 hash, never the password
    security_stamp        text        NOT NULL,           -- changes on password reset: invalidates refresh tokens
    lockout_end           timestamptz NULL,
    access_failed_count   int         NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL
);

-- Refresh tokens (Phase 6). Only a hash is stored; rotation links the old token to its replacement,
-- so reuse of a rotated token can be detected and the whole chain revoked.
CREATE TABLE refresh_tokens (
    id              uuid        PRIMARY KEY,
    user_id         uuid        NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    token_hash      bytea       NOT NULL UNIQUE,          -- SHA-256 of a 256-bit random token
    created_at      timestamptz NOT NULL,
    expires_at      timestamptz NOT NULL,
    revoked_at      timestamptz NULL,
    replaced_by_id  uuid        NULL REFERENCES refresh_tokens (id)
);
CREATE INDEX ix_refresh_tokens_user ON refresh_tokens (user_id) WHERE revoked_at IS NULL;

-- ---------------------------------------------------------------------------------------------
-- Projects and access
-- ---------------------------------------------------------------------------------------------
CREATE TABLE projects (
    id                 uuid        PRIMARY KEY,
    key                text        NOT NULL UNIQUE CHECK (key ~ '^[a-z0-9][a-z0-9-]{1,62}$'),  -- used in CI URLs
    name               text        NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    repository_url     text        NULL     CHECK (repository_url ~ '^https://'),
    default_branch     text        NOT NULL DEFAULT 'main',
    -- Quality gate policy; NULL = threshold not enforced
    gate_max_critical  int         NULL CHECK (gate_max_critical >= 0),
    gate_max_high      int         NULL CHECK (gate_max_high >= 0),
    gate_max_medium    int         NULL CHECK (gate_max_medium >= 0),
    last_scan_number   int         NOT NULL DEFAULT 0,    -- incremented under the project ingestion lock
    created_at         timestamptz NOT NULL,
    created_by_id      uuid        NOT NULL REFERENCES users (id)
);

CREATE TABLE project_members (
    project_id  uuid        NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    user_id     uuid        NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    role        text        NOT NULL CHECK (role IN ('Viewer', 'Developer', 'SecurityLead', 'Admin')),
    added_at    timestamptz NOT NULL,
    PRIMARY KEY (project_id, user_id)
);
CREATE INDEX ix_project_members_user ON project_members (user_id);  -- "my projects"

-- Machine-to-machine keys for CI. Format shown once to the user: shk_<prefix>_<secret>.
-- The prefix is public and indexed for lookup; the full key is stored only as SHA-256.
CREATE TABLE api_keys (
    id             uuid        PRIMARY KEY,
    project_id     uuid        NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    name           text        NOT NULL CHECK (length(name) BETWEEN 1 AND 60),
    prefix         char(8)     NOT NULL UNIQUE,
    key_hash       bytea       NOT NULL CHECK (octet_length(key_hash) = 32),
    scopes         text[]      NOT NULL CHECK (scopes <@ ARRAY['scans:write', 'gate:read']::text[] AND cardinality(scopes) > 0),
    created_at     timestamptz NOT NULL,
    created_by_id  uuid        NOT NULL REFERENCES users (id),
    expires_at     timestamptz NULL,
    last_used_at   timestamptz NULL,
    revoked_at     timestamptz NULL,
    UNIQUE (project_id, name)
);

-- ---------------------------------------------------------------------------------------------
-- Scans
-- ---------------------------------------------------------------------------------------------
CREATE TABLE scans (
    id                  uuid        PRIMARY KEY,
    project_id          uuid        NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    number              int         NOT NULL CHECK (number > 0),
    branch              text        NOT NULL,
    commit_sha          text        NULL CHECK (commit_sha ~ '^[0-9a-f]{7,64}$'),
    status              text        NOT NULL CHECK (status IN ('Completed', 'Rejected')),
    uploaded_at         timestamptz NOT NULL,
    uploaded_by_user_id uuid        NULL REFERENCES users (id),
    uploaded_by_key_id  uuid        NULL REFERENCES api_keys (id),
    sarif_sha256        bytea       NOT NULL,             -- detects re-upload of an identical file
    -- Diff result, written once when the scan completes (scans are immutable afterwards)
    total_count         int         NOT NULL DEFAULT 0,
    new_count           int         NOT NULL DEFAULT 0,
    existing_count      int         NOT NULL DEFAULT 0,
    reopened_count      int         NOT NULL DEFAULT 0,
    resolved_count      int         NOT NULL DEFAULT 0,
    critical_count      int         NOT NULL DEFAULT 0,   -- present findings by severity: feeds trends
    high_count          int         NOT NULL DEFAULT 0,
    medium_count        int         NOT NULL DEFAULT 0,
    low_count           int         NOT NULL DEFAULT 0,
    -- Quality gate snapshot: what CI was told, with the policy that was in force at that moment
    gate_result         text        NULL CHECK (gate_result IN ('Passed', 'Failed')),
    gate_evaluation     jsonb       NULL,                 -- {policy:{...}, counts:{...}, reasons:[...]}
    UNIQUE (project_id, number),
    CHECK (num_nonnulls(uploaded_by_user_id, uploaded_by_key_id) = 1)
);
CREATE INDEX ix_scans_project_uploaded ON scans (project_id, uploaded_at DESC);
CREATE INDEX ix_scans_project_sha ON scans (project_id, sarif_sha256);

-- One row per tool run inside the SARIF file (a file can contain several runs).
CREATE TABLE scan_tools (
    scan_id       uuid NOT NULL REFERENCES scans (id) ON DELETE CASCADE,
    tool_name     text NOT NULL,
    tool_version  text NULL,
    result_count  int  NOT NULL,
    PRIMARY KEY (scan_id, tool_name)
);

-- Raw upload, compressed. Kept so findings can be re-derived when the fingerprint algorithm changes.
CREATE TABLE scan_artifacts (
    scan_id          uuid   PRIMARY KEY REFERENCES scans (id) ON DELETE CASCADE,
    content_gzip     bytea  NOT NULL,
    original_bytes   bigint NOT NULL CHECK (original_bytes > 0)
);

-- ---------------------------------------------------------------------------------------------
-- Rules and findings
-- ---------------------------------------------------------------------------------------------
-- Rule metadata as reported by the tool; shared across projects. Last upload wins for descriptions.
CREATE TABLE rules (
    id                 uuid    PRIMARY KEY,
    tool_name          text    NOT NULL,
    rule_id            text    NOT NULL,                  -- tool's id, e.g. cs/sql-injection
    name               text    NULL,
    short_description  text    NULL,
    help_uri           text    NULL,
    cwe                text[]  NOT NULL DEFAULT '{}',
    UNIQUE (tool_name, rule_id)
);

-- A logical finding: one row per fingerprint per project, alive across many scans.
-- Current-state columns are denormalized so the grid can filter and sort without joins to history.
CREATE TABLE findings (
    id                        uuid        PRIMARY KEY,
    project_id                uuid        NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    rule_id                   uuid        NOT NULL REFERENCES rules (id),
    fingerprint               text        NOT NULL,       -- "v1:<sha256 hex>": version travels with the value
    severity                  text        NOT NULL CHECK (severity IN ('Critical', 'High', 'Medium', 'Low')),
    -- Text sorts alphabetically (Critical, High, Low, Medium). A stored rank gives the grid an index it can walk.
    severity_rank             smallint    GENERATED ALWAYS AS (
                                  CASE severity WHEN 'Critical' THEN 4 WHEN 'High' THEN 3 WHEN 'Medium' THEN 2 ELSE 1 END
                              ) STORED,
    lifecycle                 text        NOT NULL CHECK (lifecycle IN ('New', 'Existing', 'Reopened', 'Resolved')),
    triage_status             text        NOT NULL DEFAULT 'Untriaged'
                                          CHECK (triage_status IN ('Untriaged', 'Confirmed', 'FalsePositive', 'AcceptedRisk')),
    accepted_risk_expires_at  timestamptz NULL,
    -- Location and message as of the last scan the finding appeared in
    file_path                 text        NOT NULL,
    start_line                int         NULL CHECK (start_line > 0),
    message                   text        NOT NULL,
    first_seen_scan_id        uuid        NOT NULL REFERENCES scans (id),
    last_seen_scan_id         uuid        NOT NULL REFERENCES scans (id),
    first_seen_at             timestamptz NOT NULL,
    last_seen_at              timestamptz NOT NULL,
    UNIQUE (project_id, fingerprint),
    CHECK ((triage_status = 'AcceptedRisk') = (accepted_risk_expires_at IS NOT NULL))
);
-- Grid default: open findings of a project, most severe first, paged. The id makes the order total.
CREATE INDEX ix_findings_open_severity ON findings (project_id, severity_rank DESC, id) WHERE lifecycle <> 'Resolved';
CREATE INDEX ix_findings_project_last_seen ON findings (project_id, last_seen_at DESC);
CREATE INDEX ix_findings_rule ON findings (rule_id);
-- "Contains" search on path and message
CREATE INDEX ix_findings_file_trgm ON findings USING gin (file_path gin_trgm_ops);
CREATE INDEX ix_findings_message_trgm ON findings USING gin (message gin_trgm_ops);
-- Background job: find accepted risks that are about to lapse
CREATE INDEX ix_findings_risk_expiry ON findings (accepted_risk_expires_at) WHERE triage_status = 'AcceptedRisk';

-- Presence of a finding in a scan. This is the history behind "first seen", "reopened" and the line drift.
CREATE TABLE finding_occurrences (
    scan_id               uuid  NOT NULL REFERENCES scans (id) ON DELETE CASCADE,
    finding_id            uuid  NOT NULL REFERENCES findings (id) ON DELETE CASCADE,
    change                text  NOT NULL CHECK (change IN ('New', 'Existing', 'Reopened')),
    severity              text  NOT NULL CHECK (severity IN ('Critical', 'High', 'Medium', 'Low')),
    file_path             text  NOT NULL,
    start_line            int   NULL CHECK (start_line > 0),
    start_column          int   NULL CHECK (start_column > 0),
    end_line              int   NULL,
    message               text  NOT NULL,
    snippet               text  NULL,                     -- region.snippet from SARIF when the tool provides it
    partial_fingerprints  jsonb NULL,                     -- tool-provided fingerprints, kept for future algorithms
    PRIMARY KEY (scan_id, finding_id)
);
CREATE INDEX ix_occurrences_finding ON finding_occurrences (finding_id, scan_id);

-- A finding that was present in the previous scan and is absent from this one.
-- Kept separately from occurrences: a resolution has no location, and mixing the two would
-- make "occurrence" mean two different things.
CREATE TABLE finding_resolutions (
    scan_id     uuid NOT NULL REFERENCES scans (id) ON DELETE CASCADE,
    finding_id  uuid NOT NULL REFERENCES findings (id) ON DELETE CASCADE,
    PRIMARY KEY (scan_id, finding_id)
);
CREATE INDEX ix_resolutions_finding ON finding_resolutions (finding_id);

-- ---------------------------------------------------------------------------------------------
-- Triage and audit (append-only)
-- ---------------------------------------------------------------------------------------------
CREATE TABLE triage_decisions (
    id              uuid        PRIMARY KEY,
    finding_id      uuid        NOT NULL REFERENCES findings (id) ON DELETE CASCADE,
    status          text        NOT NULL CHECK (status IN ('Untriaged', 'Confirmed', 'FalsePositive', 'AcceptedRisk')),
    reason          text        NOT NULL CHECK (length(reason) <= 1000),
    expires_at      timestamptz NULL,
    decided_by_id   uuid        NULL REFERENCES users (id),   -- NULL = system (e.g. acceptance lapsed)
    decided_at      timestamptz NOT NULL,
    CHECK ((status = 'AcceptedRisk') = (expires_at IS NOT NULL)),
    CHECK (status = 'Confirmed' OR decided_by_id IS NULL OR length(reason) >= 10)
);
CREATE INDEX ix_triage_finding ON triage_decisions (finding_id, decided_at DESC);

CREATE TABLE audit_events (
    id           bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    occurred_at  timestamptz NOT NULL,
    actor_type   text        NOT NULL CHECK (actor_type IN ('User', 'ApiKey', 'System', 'Anonymous')),
    actor_id     uuid        NULL,
    project_id   uuid        NULL REFERENCES projects (id) ON DELETE SET NULL,
    action       text        NOT NULL,                     -- e.g. scan.uploaded, finding.triaged, apikey.revoked
    target_type  text        NULL,
    target_id    text        NULL,
    ip_address   inet        NULL,
    data         jsonb       NOT NULL DEFAULT '{}'::jsonb  -- never contains secrets or raw keys
);
CREATE INDEX ix_audit_project_time ON audit_events (project_id, occurred_at DESC);
CREATE INDEX ix_audit_actor_time ON audit_events (actor_id, occurred_at DESC);
