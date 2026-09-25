using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Triage;

namespace SarifHub.Application.Findings;

/// <summary>
/// A row of the findings grid (<c>FindingListItem</c>). <see cref="Line"/> is <c>null</c> when the tool reported
/// no region (allowed by SARIF).
/// </summary>
public record FindingListItemDto(
    Guid Id,
    Severity Severity,
    Lifecycle Lifecycle,
    TriageStatus Triage,
    string Tool,
    string RuleId,
    string RuleName,
    string FilePath,
    int? Line,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int FirstSeenScan,
    int LastSeenScan,
    DateTimeOffset? AcceptedRiskExpiresAt);

/// <summary>Source lines to show for a finding (<c>CodeSnippet</c>).</summary>
public sealed record CodeSnippetDto(int StartLine, int HighlightLine, IReadOnlyList<string> Lines);

/// <summary>One scan the finding appeared in (<c>FindingOccurrence</c>).</summary>
public sealed record FindingOccurrenceDto(Guid ScanId, int ScanNumber, DateTimeOffset SeenAt, string FilePath, int? Line, Lifecycle LifecycleInScan);

/// <summary>A triage history entry (<c>TriageDecision</c>). <see cref="DecidedBy"/> is a display name.</summary>
public sealed record TriageDecisionDto(Guid Id, TriageStatus Status, string Reason, DateTimeOffset? ExpiresAt, string DecidedBy, DateTimeOffset DecidedAt);

/// <summary>Everything the finding details page shows (<c>FindingDetail</c>).</summary>
public sealed record FindingDetailDto(
    Guid Id,
    Severity Severity,
    Lifecycle Lifecycle,
    TriageStatus Triage,
    string Tool,
    string RuleId,
    string RuleName,
    string FilePath,
    int? Line,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int FirstSeenScan,
    int LastSeenScan,
    DateTimeOffset? AcceptedRiskExpiresAt,
    string Message,
    string RuleDescription,
    IReadOnlyList<string> Cwe,
    string Fingerprint,
    CodeSnippetDto Snippet,
    IReadOnlyList<FindingOccurrenceDto> Occurrences,
    IReadOnlyList<TriageDecisionDto> TriageHistory)
    : FindingListItemDto(Id, Severity, Lifecycle, Triage, Tool, RuleId, RuleName, FilePath, Line, FirstSeenAt, LastSeenAt, FirstSeenScan, LastSeenScan, AcceptedRiskExpiresAt);

/// <summary>A finding detail with its row version, which the API returns as an ETag.</summary>
public sealed record VersionedFindingDetail(FindingDetailDto Finding, uint Version);
