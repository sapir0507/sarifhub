\timing off
SELECT setseed(0.42);
-- 40 projects: one large, 39 smaller neighbours so project filters must be selective
INSERT INTO projects (id,key,name,created_at,created_by_id)
SELECT gen_random_uuid(), 'proj-'||g, 'Project '||g, now(), '00000000-0000-0000-0000-000000000001' FROM generate_series(1,39) g;
UPDATE projects SET last_scan_number = 60 WHERE key='payments-api';

INSERT INTO rules (id,tool_name,rule_id,name)
SELECT gen_random_uuid(), CASE WHEN g%2=0 THEN 'CodeQL' ELSE 'Semgrep' END, 'rule-'||g, 'Rule '||g FROM generate_series(1,80) g;

-- 60 scans for the large project, 20 for each neighbour
INSERT INTO scans (id,project_id,number,branch,status,uploaded_at,uploaded_by_key_id,sarif_sha256)
SELECT gen_random_uuid(), p.id, n, 'main','Completed', now() - (61-n)*interval '1 day',
       (SELECT id FROM api_keys LIMIT 1), sha256((p.id::text||n)::bytea)
FROM projects p CROSS JOIN LATERAL generate_series(CASE WHEN p.key='payments-api' THEN 2 ELSE 1 END, CASE WHEN p.key='payments-api' THEN 60 ELSE 20 END) n;

-- Findings: 20,000 in the large project, 1,000 in each neighbour (59,000 total)
CREATE TEMP TABLE tmp_f AS
SELECT gen_random_uuid() id, p.id project_id, p.key,
       (SELECT id FROM rules ORDER BY random() LIMIT 1 OFFSET 0) rule_id, g,
       1 + floor(random()*CASE WHEN p.key='payments-api' THEN 55 ELSE 18 END)::int s0,
       random() r1, random() r2, random() r3
FROM projects p CROSS JOIN LATERAL generate_series(1, CASE WHEN p.key='payments-api' THEN 20000 ELSE 1000 END) g;
UPDATE tmp_f SET rule_id = (SELECT id FROM rules OFFSET (g % 80) LIMIT 1);

INSERT INTO findings (id,project_id,rule_id,fingerprint,severity,lifecycle,triage_status,accepted_risk_expires_at,file_path,start_line,message,
                      first_seen_scan_id,last_seen_scan_id,first_seen_at,last_seen_at)
SELECT f.id, f.project_id, f.rule_id, 'v1:'||encode(sha256(f.id::text::bytea),'hex'),
       CASE WHEN f.r1<0.05 THEN 'Critical' WHEN f.r1<0.25 THEN 'High' WHEN f.r1<0.6 THEN 'Medium' ELSE 'Low' END,
       CASE WHEN f.r2<0.55 THEN 'Resolved' WHEN f.r2<0.9 THEN 'Existing' WHEN f.r2<0.97 THEN 'New' ELSE 'Reopened' END,
       CASE WHEN f.r3<0.1 THEN 'FalsePositive' WHEN f.r3<0.16 THEN 'AcceptedRisk' WHEN f.r3<0.5 THEN 'Confirmed' ELSE 'Untriaged' END,
       CASE WHEN f.r3>=0.1 AND f.r3<0.16 THEN now() + (f.g % 120 - 20) * interval '1 day' END,
       'src/module'||(f.g%40)||'/sub'||(f.g%7)||'/File'||(f.g%300)||'.cs', 1+(f.g%900),
       'Message about user input number '||f.g,
       s.id, s.id, s.uploaded_at, s.uploaded_at
FROM tmp_f f JOIN scans s ON s.project_id=f.project_id AND s.number = GREATEST(f.s0, CASE WHEN f.key='payments-api' THEN 2 ELSE 1 END);

-- Occurrences for the large project: contiguous presence from the first scan to a random later scan
INSERT INTO finding_occurrences (scan_id,finding_id,change,severity,file_path,start_line,message)
SELECT s.id, f.id, CASE WHEN s.number = fs.number THEN 'New' ELSE 'Existing' END, f.severity, f.file_path, f.start_line, f.message
FROM findings f
JOIN projects p ON p.id=f.project_id AND p.key='payments-api'
JOIN scans fs ON fs.id=f.first_seen_scan_id
JOIN scans s ON s.project_id=f.project_id AND s.number BETWEEN fs.number AND LEAST(60, fs.number + 5 + (abs(hashtext(f.id::text)) % 50));

ANALYZE;
SELECT (SELECT count(*) FROM findings) findings, (SELECT count(*) FROM finding_occurrences) occurrences, (SELECT count(*) FROM scans) scans;
