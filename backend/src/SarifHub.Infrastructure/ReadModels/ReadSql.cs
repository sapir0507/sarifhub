namespace SarifHub.Infrastructure.ReadModels;

/// <summary>
/// SQL shared by several read models. Keeping the fragments here means a rule such as "what counts as active"
/// is written once. Every value is a bound parameter; the only text composed at run time is chosen from these
/// constants by code, never taken from a request.
/// </summary>
internal static class ReadSql
{
    /// <summary>
    /// A finding is suppressed — excluded from active counts and the quality gate — when it is a false positive or an
    /// accepted risk that has not expired at <c>@now</c>. Same rule as <c>Finding.IsSuppressed</c> in the domain.
    /// </summary>
    public const string Suppressed =
        "(f.triage_status = 'FalsePositive' OR (f.triage_status = 'AcceptedRisk' AND f.accepted_risk_expires_at > @now))";

    /// <summary>Active severity counts of a project (<c>c.critical … c.low</c>), as a lateral subquery over <c>p</c>.</summary>
    public const string ActiveCountsLateral = $$"""
        CROSS JOIN LATERAL (
            SELECT count(*) FILTER (WHERE f.severity = 'Critical')::int AS critical,
                   count(*) FILTER (WHERE f.severity = 'High')::int     AS high,
                   count(*) FILTER (WHERE f.severity = 'Medium')::int   AS medium,
                   count(*) FILTER (WHERE f.severity = 'Low')::int      AS low
            FROM findings f
            WHERE f.project_id = p.id AND f.lifecycle <> 'Resolved' AND NOT {{Suppressed}}
        ) c
        """;

    /// <summary>Projects the user <c>@userId</c> is a member of, with role, latest scan and active counts.</summary>
    public const string ProjectSummaries = $$"""
        SELECT p.id, p.key, p.name, p.repository_url, p.default_branch, m.role AS my_role,
               s.id AS last_scan_id, s.number AS last_scan_number, s.uploaded_at AS last_scan_uploaded_at,
               s.gate_evaluation::text AS last_scan_gate,
               c.critical, c.high, c.medium, c.low
        FROM projects p
        JOIN project_members m ON m.project_id = p.id AND m.user_id = @userId
        LEFT JOIN scans s ON s.project_id = p.id AND s.number = p.last_scan_number AND s.status = 'Completed'
        {{ActiveCountsLateral}}
        """;

    /// <summary>Completed scans of <c>@projectId</c> with uploader name and tools. Append WHERE/ORDER as needed.</summary>
    public const string ScanSummaries = """
        SELECT s.id, s.number, s.project_id, s.branch, s.commit_sha, s.uploaded_at,
               COALESCE(u.display_name, 'CI (key: ' || k.name || ')') AS uploaded_by,
               s.total_count, s.new_count, s.existing_count, s.resolved_count, s.reopened_count,
               s.critical_count, s.high_count, s.medium_count, s.low_count,
               s.gate_evaluation::text AS gate,
               (SELECT COALESCE(json_agg(json_build_object('name', t.tool_name, 'version', t.tool_version) ORDER BY t.tool_name), '[]'::json)::text
                FROM scan_tools t WHERE t.scan_id = s.id) AS tools
        FROM scans s
        LEFT JOIN users u ON u.id = s.uploaded_by_user_id
        LEFT JOIN api_keys k ON k.id = s.uploaded_by_key_id
        WHERE s.project_id = @projectId AND s.status = 'Completed'
        """;

    /// <summary>One trend point per completed scan of <c>@projectId</c>, oldest first. Reads only scan snapshots.</summary>
    public const string TrendPoints = """
        SELECT s.number AS scan_number, s.uploaded_at,
               s.critical_count, s.high_count, s.medium_count, s.low_count,
               s.new_count, s.resolved_count, s.reopened_count,
               (SELECT COALESCE(json_object_agg(t.tool_name, t.result_count ORDER BY t.tool_name), '{}'::json)::text
                FROM scan_tools t WHERE t.scan_id = s.id) AS by_tool
        FROM scans s
        WHERE s.project_id = @projectId AND s.status = 'Completed'
        ORDER BY s.number
        """;
}
