using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Triage;

namespace SarifHub.Application.Findings;

/// <summary>Sortable columns of the findings grid. The SQL for each is chosen by the read model, never taken from the request.</summary>
public enum FindingSortField
{
    /// <summary>Severity rank (Critical highest).</summary>
    Severity,

    /// <summary>Lifecycle rank (New, Reopened, Existing, Resolved).</summary>
    Lifecycle,

    /// <summary>Triage rank (Untriaged, Confirmed, AcceptedRisk, FalsePositive).</summary>
    Triage,

    /// <summary>Tool name.</summary>
    Tool,

    /// <summary>The tool's rule id.</summary>
    RuleId,

    /// <summary>File path.</summary>
    FilePath,

    /// <summary>Line number.</summary>
    Line,

    /// <summary>First seen time.</summary>
    FirstSeenAt,

    /// <summary>Last seen time.</summary>
    LastSeenAt,
}

/// <summary>Sort direction.</summary>
public enum SortDirection
{
    /// <summary>Ascending.</summary>
    Asc,

    /// <summary>Descending.</summary>
    Desc,
}

/// <summary>
/// A validated findings grid request. Build it with <see cref="FindingsQueryParser"/>; every value is typed,
/// so nothing from the request is ever interpolated into SQL.
/// </summary>
/// <param name="Page">0-based page.</param>
/// <param name="PageSize">25, 50 or 100.</param>
/// <param name="Sort">Sort column.</param>
/// <param name="Direction">Sort direction. Ties are broken by id, so paging is stable.</param>
/// <param name="Severities">Severity filter; empty = all.</param>
/// <param name="Statuses">Lifecycle filter; empty = all. With <paramref name="ScanNumber"/>, applies to the lifecycle in that scan.</param>
/// <param name="TriageStatuses">Triage filter; empty = all.</param>
/// <param name="Tools">Tool filter; empty = all.</param>
/// <param name="Search">Case-insensitive "contains" on rule id, rule name, file path and message.</param>
/// <param name="ScanNumber">Limit to findings present in or resolved by this scan.</param>
public sealed record FindingsQuery(
    int Page,
    int PageSize,
    FindingSortField Sort,
    SortDirection Direction,
    IReadOnlyList<Severity> Severities,
    IReadOnlyList<Lifecycle> Statuses,
    IReadOnlyList<TriageStatus> TriageStatuses,
    IReadOnlyList<string> Tools,
    string? Search,
    int? ScanNumber);
