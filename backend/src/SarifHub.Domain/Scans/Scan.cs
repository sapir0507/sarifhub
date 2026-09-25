using System.Text.RegularExpressions;
using SarifHub.Domain.Common;
using SarifHub.Domain.Gate;
using SarifHub.Domain.Projects;

namespace SarifHub.Domain.Scans;

/// <summary>
/// One uploaded SARIF file, processed. A completed scan is an immutable snapshot (data model §3.3):
/// its diff, severity counts and quality gate result are written once and never change.
/// </summary>
public sealed partial class Scan
{
    private readonly List<ScanTool> _tools = [];
    private byte[] _sarifSha256 = [];

    private Scan()
    {
        Branch = string.Empty;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The project.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Per-project, gap-free number: people and CI logs say "scan #42".</summary>
    public int Number { get; private set; }

    /// <summary>Branch the scan ran on.</summary>
    public string Branch { get; private set; }

    /// <summary>Commit SHA (7–64 lowercase hex characters), when known.</summary>
    public string? CommitSha { get; private set; }

    /// <summary>Final state. Unset (invalid) until <see cref="Complete"/> is called.</summary>
    public ScanStatus Status { get; private set; }

    /// <summary>When the file was uploaded (UTC).</summary>
    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>Uploading user, for manual uploads.</summary>
    public Guid? UploadedByUserId { get; private set; }

    /// <summary>Uploading API key, for CI uploads.</summary>
    public Guid? UploadedByKeyId { get; private set; }

    /// <summary>SHA-256 of the uploaded file: detects re-uploads of the same file.</summary>
    public IReadOnlyList<byte> SarifSha256 => _sarifSha256;

    /// <summary>Findings present in this scan.</summary>
    public int TotalCount { get; private set; }

    /// <summary>Findings present for the first time.</summary>
    public int NewCount { get; private set; }

    /// <summary>Findings present in this and the previous scan.</summary>
    public int ExistingCount { get; private set; }

    /// <summary>Findings present again after having been resolved.</summary>
    public int ReopenedCount { get; private set; }

    /// <summary>Findings present in the previous scan and absent from this one.</summary>
    public int ResolvedCount { get; private set; }

    /// <summary>Present critical findings (feeds trends).</summary>
    public int CriticalCount { get; private set; }

    /// <summary>Present high findings.</summary>
    public int HighCount { get; private set; }

    /// <summary>Present medium findings.</summary>
    public int MediumCount { get; private set; }

    /// <summary>Present low findings.</summary>
    public int LowCount { get; private set; }

    /// <summary>Quality gate outcome, set on completion.</summary>
    public GateResult? GateResult { get; private set; }

    /// <summary>Policy, counts and reasons behind <see cref="GateResult"/>.</summary>
    public GateEvaluation? GateEvaluation { get; private set; }

    /// <summary>Tool runs contained in the upload.</summary>
    public IReadOnlyList<ScanTool> Tools => _tools;

    /// <summary>The diff against the previous scan.</summary>
    public ScanCounts Diff => new(TotalCount, NewCount, ExistingCount, ReopenedCount, ResolvedCount);

    /// <summary>Present findings by severity.</summary>
    public SeverityCounts PresentBySeverity => new(CriticalCount, HighCount, MediumCount, LowCount);

    /// <summary>Whether <see cref="Complete"/> has been called.</summary>
    public bool IsCompleted => Status == ScanStatus.Completed;

    /// <summary>
    /// Starts a scan for <paramref name="project"/>, taking the project's next scan number.
    /// The caller must hold the project's ingestion lock (ADR 0006).
    /// </summary>
    public static Scan Start(
        Project project,
        string branch,
        string? commitSha,
        IReadOnlyList<byte> sarifSha256,
        ScanUploader uploader,
        DateTimeOffset uploadedAt)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);
        ArgumentNullException.ThrowIfNull(sarifSha256);

        if (commitSha is not null && !CommitShaPattern().IsMatch(commitSha))
        {
            throw new DomainException("A commit SHA is 7–64 lowercase hexadecimal characters.");
        }

        if (sarifSha256.Count != 32)
        {
            throw new DomainException("The SARIF hash must be a SHA-256 value (32 bytes).");
        }

        if (uploader.UserId is null == uploader.ApiKeyId is null)
        {
            throw new DomainException("A scan has exactly one uploader: a user or an API key.");
        }

        return new Scan
        {
            Id = Guid.CreateVersion7(uploadedAt),
            ProjectId = project.Id,
            Number = project.AllocateScanNumber(),
            Branch = branch.Trim(),
            CommitSha = commitSha,
            UploadedAt = uploadedAt,
            UploadedByUserId = uploader.UserId,
            UploadedByKeyId = uploader.ApiKeyId,
            _sarifSha256 = [.. sarifSha256],
        };
    }

    /// <summary>Records a tool run found in the upload.</summary>
    public void AddTool(string toolName, string? toolVersion, int resultCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentOutOfRangeException.ThrowIfNegative(resultCount);
        EnsureNotCompleted();
        if (_tools.Exists(t => t.ToolName == toolName))
        {
            throw new DomainException($"Tool '{toolName}' is already recorded for this scan.");
        }

        _tools.Add(new ScanTool(Id, toolName, toolVersion, resultCount));
    }

    /// <summary>Writes the diff, the severity counts and the gate snapshot. Allowed once.</summary>
    public void Complete(ScanCounts diff, SeverityCounts presentBySeverity, GateEvaluation gate)
    {
        ArgumentNullException.ThrowIfNull(diff);
        ArgumentNullException.ThrowIfNull(presentBySeverity);
        ArgumentNullException.ThrowIfNull(gate);
        EnsureNotCompleted();

        if (diff.Total != diff.New + diff.Existing + diff.Reopened)
        {
            throw new DomainException("Total must equal new + existing + reopened.");
        }

        if (presentBySeverity.Total != diff.Total)
        {
            throw new DomainException("Severity counts must add up to the total.");
        }

        (TotalCount, NewCount, ExistingCount, ReopenedCount, ResolvedCount) =
            (diff.Total, diff.New, diff.Existing, diff.Reopened, diff.Resolved);
        (CriticalCount, HighCount, MediumCount, LowCount) =
            (presentBySeverity.Critical, presentBySeverity.High, presentBySeverity.Medium, presentBySeverity.Low);
        GateResult = gate.Result;
        GateEvaluation = gate;
        Status = ScanStatus.Completed;
    }

    private void EnsureNotCompleted()
    {
        if (IsCompleted)
        {
            throw new DomainException("A completed scan cannot change.");
        }
    }

    [GeneratedRegex("^[0-9a-f]{7,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex CommitShaPattern();
}
