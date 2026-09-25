using Dapper;
using Npgsql;
using SarifHub.Application.Common;
using SarifHub.Application.Dashboard;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>
/// The dashboard in one database round trip: five statements in a single batch (Dapper <c>QueryMultiple</c>).
/// The triage counts are one pass over the project's open findings with <c>FILTER</c> (data model §4).
/// </summary>
internal sealed class DashboardQuery(NpgsqlDataSource dataSource) : IDashboardQuery
{
    private static readonly TimeSpan ExpiringSoonWindow = TimeSpan.FromDays(14);

    private const string Sql = $$"""
        {{ReadSql.ProjectSummaries}}
        WHERE p.id = @projectId;

        SELECT count(*) FILTER (WHERE NOT {{ReadSql.Suppressed}} AND f.severity = 'Critical')::int AS critical,
               count(*) FILTER (WHERE NOT {{ReadSql.Suppressed}} AND f.severity = 'High')::int     AS high,
               count(*) FILTER (WHERE NOT {{ReadSql.Suppressed}} AND f.severity = 'Medium')::int   AS medium,
               count(*) FILTER (WHERE NOT {{ReadSql.Suppressed}} AND f.severity = 'Low')::int      AS low,
               count(*) FILTER (WHERE f.triage_status = 'Untriaged')::int                           AS pending_triage,
               count(*) FILTER (WHERE f.triage_status = 'AcceptedRisk' AND f.accepted_risk_expires_at > @now)::int AS accepted_risk,
               count(*) FILTER (WHERE f.triage_status = 'AcceptedRisk' AND f.accepted_risk_expires_at > @now
                                  AND f.accepted_risk_expires_at <= @expiringBefore)::int          AS accepted_risk_expiring_soon,
               count(*) FILTER (WHERE f.triage_status = 'FalsePositive')::int                       AS false_positive
        FROM findings f
        WHERE f.project_id = @projectId AND f.lifecycle <> 'Resolved';

        SELECT gate_max_critical AS max_critical, gate_max_high AS max_high, gate_max_medium AS max_medium
        FROM projects WHERE id = @projectId;

        {{ReadSql.TrendPoints}};

        {{ReadSql.ScanSummaries}}
        ORDER BY s.number DESC
        LIMIT 6;
        """;

    public async Task<ProjectDashboardDto?> GetAsync(Guid projectId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            Sql,
            new { projectId, userId, now, expiringBefore = now + ExpiringSoonWindow },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var project = await results.ReadSingleOrDefaultAsync<ProjectSummaryRow>().ConfigureAwait(false);
        if (project is null)
        {
            return null; // not a member, or no such project
        }

        var counts = await results.ReadSingleAsync<DashboardCountsRow>().ConfigureAwait(false);
        var policy = await results.ReadSingleAsync<GatePolicyRow>().ConfigureAwait(false);
        var trend = (await results.ReadAsync<TrendPointRow>().ConfigureAwait(false)).Select(r => r.ToDto()).ToList();
        var recent = (await results.ReadAsync<ScanSummaryRow>().ConfigureAwait(false)).Select(r => r.ToSummary()).ToList();

        var active = new SeverityCountsDto(counts.Critical, counts.High, counts.Medium, counts.Low);
        return new ProjectDashboardDto(
            project.ToDto(),
            active,
            active.Critical + active.High + active.Medium + active.Low,
            recent.FirstOrDefault(),
            counts.PendingTriage,
            counts.AcceptedRisk,
            counts.AcceptedRiskExpiringSoon,
            counts.FalsePositive,
            new QualityGatePolicyDto(policy.MaxCritical, policy.MaxHigh, policy.MaxMedium),
            trend,
            recent);
    }

    private sealed class GatePolicyRow
    {
        public int? MaxCritical { get; init; }
        public int? MaxHigh { get; init; }
        public int? MaxMedium { get; init; }
    }

    private sealed class DashboardCountsRow
    {
        public int Critical { get; init; }
        public int High { get; init; }
        public int Medium { get; init; }
        public int Low { get; init; }
        public int PendingTriage { get; init; }
        public int AcceptedRisk { get; init; }
        public int AcceptedRiskExpiringSoon { get; init; }
        public int FalsePositive { get; init; }
    }
}
