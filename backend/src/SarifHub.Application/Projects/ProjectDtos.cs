using SarifHub.Application.Common;
using SarifHub.Domain.Projects;

namespace SarifHub.Application.Projects;

/// <summary>The latest scan as shown in the projects list (<c>ProjectSummary.lastScan</c>).</summary>
public sealed record LastScanDto(Guid Id, int Number, DateTimeOffset UploadedAt, GateEvaluationDto Gate);

/// <summary>A project in the caller's projects list (<c>ProjectSummary</c>).</summary>
public sealed record ProjectSummaryDto(
    Guid Id,
    string Key,
    string Name,
    string? RepositoryUrl,
    string DefaultBranch,
    ProjectRole MyRole,
    LastScanDto? LastScan,
    SeverityCountsDto ActiveBySeverity);
