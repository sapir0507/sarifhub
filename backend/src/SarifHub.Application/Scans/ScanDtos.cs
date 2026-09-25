using SarifHub.Application.Common;

namespace SarifHub.Application.Scans;

/// <summary>
/// A scan with its diff and stored gate result (<c>ScanSummary</c>).
/// <see cref="CommitSha"/> is <c>null</c> when the upload did not name a commit.
/// </summary>
public record ScanSummaryDto(
    Guid Id,
    int Number,
    Guid ProjectId,
    string Branch,
    string? CommitSha,
    IReadOnlyList<ToolInfoDto> Tools,
    DateTimeOffset UploadedAt,
    string UploadedBy,
    int Total,
    int NewCount,
    int ExistingCount,
    int ResolvedCount,
    int ReopenedCount,
    GateEvaluationDto Gate);

/// <summary>One scan with its predecessor and severity breakdown (<c>ScanDetail</c>).</summary>
public sealed record ScanDetailDto(
    Guid Id,
    int Number,
    Guid ProjectId,
    string Branch,
    string? CommitSha,
    IReadOnlyList<ToolInfoDto> Tools,
    DateTimeOffset UploadedAt,
    string UploadedBy,
    int Total,
    int NewCount,
    int ExistingCount,
    int ResolvedCount,
    int ReopenedCount,
    GateEvaluationDto Gate,
    int? PreviousScanNumber,
    SeverityCountsDto BySeverity)
    : ScanSummaryDto(Id, Number, ProjectId, Branch, CommitSha, Tools, UploadedAt, UploadedBy, Total, NewCount, ExistingCount, ResolvedCount, ReopenedCount, Gate);
