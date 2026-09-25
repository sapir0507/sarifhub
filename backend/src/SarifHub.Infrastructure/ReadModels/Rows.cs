using System.Text.Json;
using SarifHub.Application.Common;
using SarifHub.Application.Projects;
using SarifHub.Application.Scans;
using SarifHub.Application.Trends;
using SarifHub.Domain.Projects;
using SarifHub.Infrastructure.Persistence.Json;

namespace SarifHub.Infrastructure.ReadModels;

// Dapper row types (snake_case columns map to these properties, see DependencyInjection) and their mapping to the
// API contract. Timestamps arrive as UTC DateTime from Npgsql and leave as DateTimeOffset.

internal static class RowMapping
{
    public static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static GateEvaluationDto Gate(string json)
    {
        var stored = GateEvaluationJson.Parse(json);
        return new GateEvaluationDto(
            stored.Result,
            stored.Reasons,
            new SeverityCountsDto(stored.Counts.Critical, stored.Counts.High, stored.Counts.Medium, stored.Counts.Low));
    }

    public static IReadOnlyList<ToolInfoDto> Tools(string json) =>
        JsonSerializer.Deserialize<List<ToolInfoDto>>(json, StoredJson.Options) ?? [];

    public static IReadOnlyDictionary<string, int> ToolCounts(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, int>>(json, StoredJson.Options) ?? [];
}

internal sealed class ProjectSummaryRow
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? RepositoryUrl { get; init; }
    public string DefaultBranch { get; init; } = string.Empty;
    public ProjectRole MyRole { get; init; }
    public Guid? LastScanId { get; init; }
    public int? LastScanNumber { get; init; }
    public DateTime? LastScanUploadedAt { get; init; }
    public string? LastScanGate { get; init; }
    public int Critical { get; init; }
    public int High { get; init; }
    public int Medium { get; init; }
    public int Low { get; init; }

    public ProjectSummaryDto ToDto() => new(
        Id,
        Key,
        Name,
        RepositoryUrl,
        DefaultBranch,
        MyRole,
        LastScanId is { } scanId && LastScanNumber is { } number && LastScanUploadedAt is { } at && LastScanGate is { } gate
            ? new LastScanDto(scanId, number, RowMapping.Utc(at), RowMapping.Gate(gate))
            : null,
        new SeverityCountsDto(Critical, High, Medium, Low));
}

internal sealed class ScanSummaryRow
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public Guid ProjectId { get; init; }
    public string Branch { get; init; } = string.Empty;
    public string? CommitSha { get; init; }
    public DateTime UploadedAt { get; init; }
    public string UploadedBy { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int NewCount { get; init; }
    public int ExistingCount { get; init; }
    public int ResolvedCount { get; init; }
    public int ReopenedCount { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public string Gate { get; init; } = string.Empty;
    public string Tools { get; init; } = "[]";

    public ScanSummaryDto ToSummary() => new(
        Id, Number, ProjectId, Branch, CommitSha, RowMapping.Tools(Tools), RowMapping.Utc(UploadedAt), UploadedBy,
        TotalCount, NewCount, ExistingCount, ResolvedCount, ReopenedCount, RowMapping.Gate(Gate));

    public ScanDetailDto ToDetail() => new(
        Id, Number, ProjectId, Branch, CommitSha, RowMapping.Tools(Tools), RowMapping.Utc(UploadedAt), UploadedBy,
        TotalCount, NewCount, ExistingCount, ResolvedCount, ReopenedCount, RowMapping.Gate(Gate),
        Number > 1 ? Number - 1 : null,
        new SeverityCountsDto(CriticalCount, HighCount, MediumCount, LowCount));
}

internal sealed class TrendPointRow
{
    public int ScanNumber { get; init; }
    public DateTime UploadedAt { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public int NewCount { get; init; }
    public int ResolvedCount { get; init; }
    public int ReopenedCount { get; init; }
    public string ByTool { get; init; } = "{}";

    public TrendPointDto ToDto() => new(
        ScanNumber, RowMapping.Utc(UploadedAt), CriticalCount, HighCount, MediumCount, LowCount,
        NewCount, ResolvedCount, ReopenedCount, RowMapping.ToolCounts(ByTool));
}
