namespace SarifHub.Domain.Common;

/// <summary>Normalized severity of a finding. The same four values as the frontend and the database.</summary>
public enum Severity
{
    /// <summary>Lowest severity.</summary>
    Low = 1,

    /// <summary>Medium severity.</summary>
    Medium = 2,

    /// <summary>High severity.</summary>
    High = 3,

    /// <summary>Highest severity.</summary>
    Critical = 4,
}
