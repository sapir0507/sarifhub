using System.Text.RegularExpressions;
using SarifHub.Domain.Common;
using SarifHub.Domain.Scans;
using SarifHub.Domain.Triage;

namespace SarifHub.Domain.Findings;

/// <summary>
/// A logical finding: one problem, identified by its fingerprint, that lives across many scans.
/// The history (occurrences, resolutions, triage decisions) is the record; the current-state properties are kept
/// in step with it by the methods below, so lists and counts never have to reconstruct them (ADR 0004).
/// Which findings are present in a new scan is decided by the diff engine (Phase 5); this class applies the result.
/// </summary>
public sealed partial class Finding
{
    private readonly List<FindingOccurrence> _occurrences = [];
    private readonly List<FindingResolution> _resolutions = [];
    private readonly List<TriageDecision> _triageHistory = [];

    private Finding()
    {
        Fingerprint = string.Empty;
        FilePath = string.Empty;
        Message = string.Empty;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The project.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>The rule that reported the finding.</summary>
    public Guid RuleId { get; private set; }

    /// <summary>Stable identity across scans, <c>v1:&lt;sha256 hex&gt;</c>. The version travels with the value.</summary>
    public string Fingerprint { get; private set; }

    /// <summary>Severity as of the last scan the finding appeared in.</summary>
    public Severity Severity { get; private set; }

    /// <summary>Lifecycle after the latest scan of the project.</summary>
    public Lifecycle Lifecycle { get; private set; }

    /// <summary>Latest triage decision's status.</summary>
    public TriageStatus TriageStatus { get; private set; }

    /// <summary>End of the current accepted risk, if any.</summary>
    public DateTimeOffset? AcceptedRiskExpiresAt { get; private set; }

    /// <summary>Path as of the last scan the finding appeared in.</summary>
    public string FilePath { get; private set; }

    /// <summary>Line as of the last scan the finding appeared in.</summary>
    public int? StartLine { get; private set; }

    /// <summary>Message as of the last scan the finding appeared in.</summary>
    public string Message { get; private set; }

    /// <summary>First scan the finding appeared in.</summary>
    public Guid FirstSeenScanId { get; private set; }

    /// <summary>Last scan the finding appeared in.</summary>
    public Guid LastSeenScanId { get; private set; }

    /// <summary>Upload time of the first scan it appeared in.</summary>
    public DateTimeOffset FirstSeenAt { get; private set; }

    /// <summary>Upload time of the last scan it appeared in.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Row version for optimistic concurrency on triage (PostgreSQL <c>xmin</c>).</summary>
    public uint Version { get; private set; }

    /// <summary>Scans the finding appeared in.</summary>
    public IReadOnlyList<FindingOccurrence> Occurrences => _occurrences;

    /// <summary>Scans the finding disappeared in.</summary>
    public IReadOnlyList<FindingResolution> Resolutions => _resolutions;

    /// <summary>Triage decisions, oldest first.</summary>
    public IReadOnlyList<TriageDecision> TriageHistory => _triageHistory;

    /// <summary>A finding seen for the first time in <paramref name="scan"/>.</summary>
    public static Finding Detect(
        Guid projectId,
        Guid ruleId,
        string fingerprint,
        Severity severity,
        Scan scan,
        FindingLocation location,
        IReadOnlyDictionary<string, string>? partialFingerprints = null)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(location);

        if (!FingerprintPattern().IsMatch(fingerprint))
        {
            throw new DomainException("A fingerprint is 'v<version>:<64 lowercase hex characters>'.");
        }

        if (scan.ProjectId != projectId)
        {
            throw new DomainException("The scan belongs to another project.");
        }

        var finding = new Finding
        {
            Id = Guid.CreateVersion7(scan.UploadedAt),
            ProjectId = projectId,
            RuleId = ruleId,
            Fingerprint = fingerprint,
            TriageStatus = TriageStatus.Untriaged,
            FirstSeenScanId = scan.Id,
            FirstSeenAt = scan.UploadedAt,
        };
        finding.Record(scan, Lifecycle.New, severity, location, partialFingerprints);
        return finding;
    }

    /// <summary>
    /// The finding is present again in <paramref name="scan"/>: Existing when it was present in the previous scan,
    /// Reopened when it had been resolved.
    /// </summary>
    public Lifecycle SeenIn(
        Scan scan,
        Severity severity,
        FindingLocation location,
        IReadOnlyDictionary<string, string>? partialFingerprints = null)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(location);
        var change = Lifecycle == Lifecycle.Resolved ? Lifecycle.Reopened : Lifecycle.Existing;
        Record(scan, change, severity, location, partialFingerprints);
        return change;
    }

    /// <summary>The finding was present in the previous scan and is absent from <paramref name="scan"/>.</summary>
    public void ResolvedIn(Scan scan)
    {
        ArgumentNullException.ThrowIfNull(scan);
        EnsureNotRecorded(scan);
        if (Lifecycle == Lifecycle.Resolved)
        {
            throw new DomainException("Only a present finding can be resolved.");
        }

        _resolutions.Add(new FindingResolution(scan.Id, Id));
        Lifecycle = Lifecycle.Resolved;
    }

    /// <summary>Appends a triage decision and makes it the current triage state.</summary>
    public void Triage(TriageDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.FindingId != Id)
        {
            throw new DomainException("The decision is about another finding.");
        }

        if (_triageHistory.Count > 0 && decision.DecidedAt < _triageHistory[^1].DecidedAt)
        {
            throw new DomainException("Triage decisions are appended in time order.");
        }

        _triageHistory.Add(decision);
        TriageStatus = decision.Status;
        AcceptedRiskExpiresAt = decision.ExpiresAt;
    }

    /// <summary>
    /// Whether the finding is excluded from active counts and the quality gate at <paramref name="now"/>:
    /// a false positive, or an accepted risk that has not expired.
    /// </summary>
    public bool IsSuppressed(DateTimeOffset now) =>
        TriageStatus == TriageStatus.FalsePositive
        || (TriageStatus == TriageStatus.AcceptedRisk && AcceptedRiskExpiresAt > now);

    private void Record(
        Scan scan,
        Lifecycle change,
        Severity severity,
        FindingLocation location,
        IReadOnlyDictionary<string, string>? partialFingerprints)
    {
        EnsureNotRecorded(scan);
        if (_occurrences.Count > 0 && scan.UploadedAt < LastSeenAt)
        {
            throw new DomainException("Scans are applied in upload order.");
        }

        _occurrences.Add(new FindingOccurrence(scan.Id, Id, change, severity, location, partialFingerprints));
        Lifecycle = change;
        Severity = severity;
        FilePath = location.FilePath;
        StartLine = location.StartLine;
        Message = location.Message;
        LastSeenScanId = scan.Id;
        LastSeenAt = scan.UploadedAt;
    }

    private void EnsureNotRecorded(Scan scan)
    {
        if (_occurrences.Exists(o => o.ScanId == scan.Id) || _resolutions.Exists(r => r.ScanId == scan.Id))
        {
            throw new DomainException("The scan is already recorded for this finding.");
        }
    }

    [GeneratedRegex("^v[0-9]+:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex FingerprintPattern();
}
