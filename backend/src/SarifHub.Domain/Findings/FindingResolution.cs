namespace SarifHub.Domain.Findings;

/// <summary>
/// A finding that was present in the previous scan and absent from this one. Separate from occurrences because a
/// resolution has no location, message or severity (data model §3.2).
/// </summary>
public sealed class FindingResolution
{
    private FindingResolution()
    {
    }

    internal FindingResolution(Guid scanId, Guid findingId)
    {
        ScanId = scanId;
        FindingId = findingId;
    }

    /// <summary>The scan the finding was absent from.</summary>
    public Guid ScanId { get; private set; }

    /// <summary>The finding.</summary>
    public Guid FindingId { get; private set; }
}
