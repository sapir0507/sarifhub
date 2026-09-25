namespace SarifHub.Domain.Findings;

/// <summary>
/// Where a finding stands after the latest scan. Computed from scan history, never set by a person.
/// </summary>
public enum Lifecycle
{
    /// <summary>First seen in the latest scan.</summary>
    New = 1,

    /// <summary>Present in the latest scan and in the scan before it.</summary>
    Existing = 2,

    /// <summary>Present in the latest scan after having been resolved earlier.</summary>
    Reopened = 3,

    /// <summary>Absent from the latest scan.</summary>
    Resolved = 4,
}
