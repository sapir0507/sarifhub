namespace SarifHub.Domain.Scans;

/// <summary>Final state of an upload. Scans are stored only once processing has finished.</summary>
public enum ScanStatus
{
    /// <summary>Processed; findings and diff are recorded.</summary>
    Completed = 1,

    /// <summary>Stored for the audit trail but not processed (for example, invalid SARIF).</summary>
    Rejected = 2,
}
