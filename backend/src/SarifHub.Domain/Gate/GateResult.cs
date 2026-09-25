namespace SarifHub.Domain.Gate;

/// <summary>Outcome of a quality gate evaluation.</summary>
public enum GateResult
{
    /// <summary>All thresholds are within policy.</summary>
    Passed = 1,

    /// <summary>At least one threshold is exceeded.</summary>
    Failed = 2,
}
