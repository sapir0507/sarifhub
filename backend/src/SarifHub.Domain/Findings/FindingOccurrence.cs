using SarifHub.Domain.Common;

namespace SarifHub.Domain.Findings;

/// <summary>
/// A finding's presence in one scan, with the location and message as reported in that scan.
/// This history is what "first seen", "reopened" and line drift are derived from.
/// </summary>
public sealed class FindingOccurrence
{
    private FindingOccurrence()
    {
        FilePath = string.Empty;
        Message = string.Empty;
    }

    internal FindingOccurrence(
        Guid scanId,
        Guid findingId,
        Lifecycle change,
        Severity severity,
        FindingLocation location,
        IReadOnlyDictionary<string, string>? partialFingerprints)
    {
        if (change == Lifecycle.Resolved)
        {
            throw new DomainException("An occurrence is New, Existing or Reopened; a disappearance is a resolution.");
        }

        ScanId = scanId;
        FindingId = findingId;
        Change = change;
        Severity = severity;
        FilePath = location.FilePath;
        StartLine = location.StartLine;
        StartColumn = location.StartColumn;
        EndLine = location.EndLine;
        Message = location.Message;
        Snippet = location.Snippet;
        PartialFingerprints = partialFingerprints is null ? null : new Dictionary<string, string>(partialFingerprints, StringComparer.Ordinal);
    }

    /// <summary>The scan.</summary>
    public Guid ScanId { get; private set; }

    /// <summary>The finding.</summary>
    public Guid FindingId { get; private set; }

    /// <summary>The finding's lifecycle as computed in this scan.</summary>
    public Lifecycle Change { get; private set; }

    /// <summary>Severity as reported in this scan.</summary>
    public Severity Severity { get; private set; }

    /// <summary>Repository-relative path in this scan.</summary>
    public string FilePath { get; private set; }

    /// <summary>First line in this scan.</summary>
    public int? StartLine { get; private set; }

    /// <summary>First column in this scan.</summary>
    public int? StartColumn { get; private set; }

    /// <summary>Last line in this scan.</summary>
    public int? EndLine { get; private set; }

    /// <summary>Message in this scan.</summary>
    public string Message { get; private set; }

    /// <summary>Source snippet, when the tool provided one.</summary>
    public string? Snippet { get; private set; }

    /// <summary>Tool-provided fingerprints, kept for future fingerprint algorithms.</summary>
    public IReadOnlyDictionary<string, string>? PartialFingerprints { get; private set; }
}
