using SarifHub.Domain.Gate;

namespace SarifHub.Application.Common;

// Response shapes shared by several endpoints. Each type mirrors an interface in frontend/src/api/types.ts,
// the API contract. JSON uses camelCase property names and enum names as strings.

/// <summary>Findings per severity (<c>SeverityCounts</c>).</summary>
public sealed record SeverityCountsDto(int Critical, int High, int Medium, int Low);

/// <summary>Gate thresholds (<c>QualityGatePolicy</c>); <c>null</c> = not enforced.</summary>
public sealed record QualityGatePolicyDto(int? MaxCritical, int? MaxHigh, int? MaxMedium);

/// <summary>A stored quality gate result (<c>GateEvaluation</c>).</summary>
public sealed record GateEvaluationDto(GateResult Result, IReadOnlyList<string> Reasons, SeverityCountsDto EvaluatedCounts);

/// <summary>A tool that ran in a scan (<c>ToolInfo</c>).</summary>
public sealed record ToolInfoDto(string Name, string? Version);

/// <summary>One page of results (<c>PagedResult</c>); <see cref="Page"/> is 0-based.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
