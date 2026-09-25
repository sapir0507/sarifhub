using SarifHub.Domain.Common;

namespace SarifHub.Domain.Gate;

/// <summary>
/// Quality gate thresholds of a project. A scan fails when active findings of a severity exceed its threshold.
/// <c>null</c> means the severity is not enforced.
/// </summary>
public sealed record GatePolicy
{
    /// <summary>Creates a policy; thresholds must be zero or positive.</summary>
    public GatePolicy(int? maxCritical, int? maxHigh, int? maxMedium)
    {
        if (maxCritical < 0 || maxHigh < 0 || maxMedium < 0)
        {
            throw new DomainException("Gate thresholds cannot be negative.");
        }

        MaxCritical = maxCritical;
        MaxHigh = maxHigh;
        MaxMedium = maxMedium;
    }

    /// <summary>The default for new projects: no critical findings, at most ten high ones.</summary>
    public static GatePolicy Default { get; } = new(0, 10, null);

    /// <summary>Maximum critical findings, or <c>null</c> when not enforced.</summary>
    public int? MaxCritical { get; }

    /// <summary>Maximum high findings, or <c>null</c> when not enforced.</summary>
    public int? MaxHigh { get; }

    /// <summary>Maximum medium findings, or <c>null</c> when not enforced.</summary>
    public int? MaxMedium { get; }
}
