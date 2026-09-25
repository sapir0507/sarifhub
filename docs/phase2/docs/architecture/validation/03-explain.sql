\set p '''00000000-0000-0000-0000-0000000000a1'''
\echo '=== Q1 findings grid, default view: open, most severe first, page 1'
EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, SUMMARY ON)
SELECT f.id, f.severity, f.lifecycle, f.triage_status, r.tool_name, r.rule_id, f.file_path, f.start_line, f.first_seen_at, f.last_seen_at
FROM findings f JOIN rules r ON r.id = f.rule_id
WHERE f.project_id = :p AND f.lifecycle <> 'Resolved'
ORDER BY f.severity_rank DESC, f.id LIMIT 25 OFFSET 0;

\echo '=== Q1b same, page 200 (deep offset)'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT f.id FROM findings f
WHERE f.project_id = :p AND f.lifecycle <> 'Resolved'
ORDER BY f.severity_rank DESC, f.id LIMIT 25 OFFSET 5000;

\echo '=== Q2 total count for the pager'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT count(*) FROM findings f WHERE f.project_id = :p AND f.lifecycle <> 'Resolved' AND f.triage_status = 'Untriaged';

\echo '=== Q3 dashboard counts in one pass (FILTER)'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT count(*) FILTER (WHERE severity='Critical' AND active) critical,
       count(*) FILTER (WHERE severity='High' AND active) high,
       count(*) FILTER (WHERE severity='Medium' AND active) medium,
       count(*) FILTER (WHERE severity='Low' AND active) low,
       count(*) FILTER (WHERE triage_status='Untriaged') pending,
       count(*) FILTER (WHERE triage_status='AcceptedRisk' AND accepted_risk_expires_at > now()) accepted,
       count(*) FILTER (WHERE triage_status='FalsePositive') false_positive
FROM (SELECT severity, triage_status, accepted_risk_expires_at,
             NOT (triage_status='FalsePositive' OR (triage_status='AcceptedRisk' AND accepted_risk_expires_at > now())) active
      FROM findings WHERE project_id = :p AND lifecycle <> 'Resolved') x;

\echo '=== Q4 search: file path contains'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT f.id FROM findings f
WHERE f.project_id = :p AND f.file_path ILIKE '%module17/sub3%'
ORDER BY f.severity_rank DESC, f.id LIMIT 25;

\echo '=== Q5 scan-scoped grid: present in scan 40 plus resolved in scan 40'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
WITH s AS (SELECT id FROM scans WHERE project_id = :p AND number = 40)
SELECT f.id, o.change FROM finding_occurrences o JOIN findings f ON f.id=o.finding_id WHERE o.scan_id = (SELECT id FROM s)
UNION ALL
SELECT f.id, 'Resolved' FROM finding_resolutions r JOIN findings f ON f.id=r.finding_id WHERE r.scan_id = (SELECT id FROM s)
ORDER BY 1 LIMIT 25;

\echo '=== Q6 diff engine input: fingerprints present in the previous scan'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT f.fingerprint FROM finding_occurrences o JOIN findings f ON f.id=o.finding_id
WHERE o.scan_id = (SELECT id FROM scans WHERE project_id = :p AND number = 59);

\echo '=== Q7 finding details: occurrence history'
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT s.number, s.uploaded_at, o.start_line, o.change FROM finding_occurrences o JOIN scans s ON s.id=o.scan_id
WHERE o.finding_id = (SELECT id FROM findings WHERE project_id = :p LIMIT 1) ORDER BY s.number DESC;
EXPLAIN (ANALYZE, COSTS OFF, SUMMARY ON)
SELECT id, fingerprint, lifecycle FROM findings WHERE project_id = :p;
