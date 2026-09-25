using SarifHub.Application.Common;
using SarifHub.Application.Projects;
using SarifHub.Application.Scans;
using SarifHub.Application.Trends;

namespace SarifHub.Application.Dashboard;

/// <summary>Everything the project dashboard shows, in one response (<c>ProjectDashboard</c>).</summary>
/// <param name="Project">The project, as in the projects list.</param>
/// <param name="ActiveBySeverity">Open findings excluding false positives and unexpired accepted risks.</param>
/// <param name="TotalActive">Sum of <paramref name="ActiveBySeverity"/>.</param>
/// <param name="LatestScan">The latest scan, or <c>null</c> before the first upload.</param>
/// <param name="PendingTriage">Open findings without a triage decision.</param>
/// <param name="AcceptedRisk">Open findings with an unexpired accepted risk.</param>
/// <param name="AcceptedRiskExpiringSoon">Of those, the ones expiring within 14 days.</param>
/// <param name="FalsePositive">Open findings marked as false positives.</param>
/// <param name="GatePolicy">The project's current gate policy.</param>
/// <param name="Trend">One point per scan.</param>
/// <param name="RecentScans">Up to six latest scans, newest first.</param>
public sealed record ProjectDashboardDto(
    ProjectSummaryDto Project,
    SeverityCountsDto ActiveBySeverity,
    int TotalActive,
    ScanSummaryDto? LatestScan,
    int PendingTriage,
    int AcceptedRisk,
    int AcceptedRiskExpiringSoon,
    int FalsePositive,
    QualityGatePolicyDto GatePolicy,
    IReadOnlyList<TrendPointDto> Trend,
    IReadOnlyList<ScanSummaryDto> RecentScans);

/// <summary>Read model for the dashboard.</summary>
public interface IDashboardQuery
{
    /// <summary>The dashboard of a project the user is a member of, or <c>null</c>.</summary>
    Task<ProjectDashboardDto?> GetAsync(Guid projectId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
