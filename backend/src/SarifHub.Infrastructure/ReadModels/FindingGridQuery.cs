using System.Text;
using Dapper;
using Npgsql;
using SarifHub.Application.Common;
using SarifHub.Application.Findings;
using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Triage;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>
/// The findings grid (Dapper). Filters, sort and paging are applied in SQL. The statement is assembled from fixed
/// fragments selected by typed values; request values only ever travel as bound parameters.
/// </summary>
internal sealed class FindingGridQuery(NpgsqlDataSource dataSource) : IFindingGridQuery
{
    private static string Columns(string lifecycle) => $"""
        SELECT f.id, f.severity, {lifecycle} AS lifecycle, f.triage_status AS triage, r.tool_name AS tool, r.rule_id,
               COALESCE(r.name, r.rule_id) AS rule_name, f.file_path, f.start_line AS line,
               f.first_seen_at, f.last_seen_at, fs.number AS first_seen_scan, ls.number AS last_seen_scan,
               f.accepted_risk_expires_at
        """;

    private const string ScanScope = """
        WITH in_scan AS (
            SELECT o.finding_id, o.change AS lifecycle FROM finding_occurrences o WHERE o.scan_id = @scanId
            UNION ALL
            SELECT x.finding_id, 'Resolved' FROM finding_resolutions x WHERE x.scan_id = @scanId
        )
        """;

    public async Task<PagedResult<FindingListItemDto>?> SearchAsync(Guid projectId, FindingsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        Guid? scanId = null;
        if (query.ScanNumber is { } number)
        {
            scanId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM scans WHERE project_id = @projectId AND number = @number AND status = 'Completed'",
                new { projectId, number },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (scanId is null)
            {
                return null;
            }
        }

        // With a scan, lifecycle means "lifecycle in that scan" (as on the scan details page).
        var lifecycle = scanId is null ? "f.lifecycle" : "sc.lifecycle";
        var from = BuildFromAndWhere(query, lifecycle, scanId is not null);
        var prefix = scanId is null ? string.Empty : ScanScope;
        var parameters = new
        {
            projectId,
            scanId,
            severities = query.Severities.Select(s => s.ToString()).ToArray(),
            statuses = query.Statuses.Select(s => s.ToString()).ToArray(),
            triage = query.TriageStatuses.Select(s => s.ToString()).ToArray(),
            tools = query.Tools.ToArray(),
            search = query.Search is null ? null : $"%{EscapeLike(query.Search)}%",
            limit = query.PageSize,
            offset = query.Page * query.PageSize,
        };

        var sql = $"""
            {prefix}
            {Columns(lifecycle)}
            {from}
            ORDER BY {OrderBy(query, lifecycle)}
            LIMIT @limit OFFSET @offset;

            {prefix}
            SELECT count(*)::int
            {from};
            """;

        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        var items = (await results.ReadAsync<FindingRow>().ConfigureAwait(false)).Select(r => r.ToDto()).ToList();
        var total = await results.ReadSingleAsync<int>().ConfigureAwait(false);
        return new PagedResult<FindingListItemDto>(items, total, query.Page, query.PageSize);
    }

    private static string BuildFromAndWhere(FindingsQuery query, string lifecycle, bool scoped)
    {
        var sql = new StringBuilder("""
            FROM findings f
            JOIN rules r ON r.id = f.rule_id
            JOIN scans fs ON fs.id = f.first_seen_scan_id
            JOIN scans ls ON ls.id = f.last_seen_scan_id
            """);
        if (scoped)
        {
            sql.AppendLine().Append("JOIN in_scan sc ON sc.finding_id = f.id");
        }

        sql.AppendLine().Append("WHERE f.project_id = @projectId");
        if (query.Severities.Count > 0)
        {
            sql.AppendLine().Append("AND f.severity = ANY(@severities)");
        }

        if (query.Statuses.Count > 0)
        {
            sql.AppendLine().Append("AND ").Append(lifecycle).Append(" = ANY(@statuses)");
            if (!scoped && !query.Statuses.Contains(Lifecycle.Resolved))
            {
                // Implied by the filter, but stated so the planner can use the partial index ix_findings_open_severity.
                sql.AppendLine().Append("AND f.lifecycle <> 'Resolved'");
            }
        }

        if (query.TriageStatuses.Count > 0)
        {
            sql.AppendLine().Append("AND f.triage_status = ANY(@triage)");
        }

        if (query.Tools.Count > 0)
        {
            sql.AppendLine().Append("AND r.tool_name = ANY(@tools)");
        }

        if (query.Search is not null)
        {
            // ILIKE '%…%' on path and message is served by the trigram GIN indexes.
            sql.AppendLine().Append("AND (f.file_path ILIKE @search OR f.message ILIKE @search OR r.rule_id ILIKE @search OR r.name ILIKE @search)");
        }

        return sql.ToString();
    }

    /// <summary>The sort allow-list: every field maps to a fixed SQL expression. Ties are broken by id for stable paging.</summary>
    private static string OrderBy(FindingsQuery query, string lifecycle)
    {
        var direction = query.Direction == SortDirection.Asc ? "ASC" : "DESC";
        var expression = query.Sort switch
        {
            FindingSortField.Severity => "f.severity_rank",
            FindingSortField.Lifecycle => $"CASE {lifecycle} WHEN 'New' THEN 4 WHEN 'Reopened' THEN 3 WHEN 'Existing' THEN 2 ELSE 1 END",
            FindingSortField.Triage => "CASE f.triage_status WHEN 'Untriaged' THEN 4 WHEN 'Confirmed' THEN 3 WHEN 'AcceptedRisk' THEN 2 ELSE 1 END",
            FindingSortField.Tool => "r.tool_name",
            FindingSortField.RuleId => "r.rule_id",
            FindingSortField.FilePath => "f.file_path",
            FindingSortField.Line => "f.start_line",
            FindingSortField.FirstSeenAt => "f.first_seen_at",
            FindingSortField.LastSeenAt => "f.last_seen_at",
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Sort, "Unknown sort field."),
        };
        var nulls = query.Sort == FindingSortField.Line ? " NULLS LAST" : string.Empty;
        return $"{expression} {direction}{nulls}, f.id";
    }

    /// <summary>Escapes LIKE wildcards so the search text matches literally (backslash is PostgreSQL's default escape).</summary>
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class FindingRow
    {
        public Guid Id { get; init; }
        public Severity Severity { get; init; }
        public Lifecycle Lifecycle { get; init; }
        public TriageStatus Triage { get; init; }
        public string Tool { get; init; } = string.Empty;
        public string RuleId { get; init; } = string.Empty;
        public string RuleName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public int? Line { get; init; }
        public DateTime FirstSeenAt { get; init; }
        public DateTime LastSeenAt { get; init; }
        public int FirstSeenScan { get; init; }
        public int LastSeenScan { get; init; }
        public DateTime? AcceptedRiskExpiresAt { get; init; }

        public FindingListItemDto ToDto() => new(
            Id, Severity, Lifecycle, Triage, Tool, RuleId, RuleName, FilePath, Line,
            RowMapping.Utc(FirstSeenAt), RowMapping.Utc(LastSeenAt), FirstSeenScan, LastSeenScan,
            AcceptedRiskExpiresAt is { } expires ? RowMapping.Utc(expires) : null);
    }
}
