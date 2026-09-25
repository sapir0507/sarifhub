using System.Globalization;
using SarifHub.Domain.Common;

namespace SarifHub.Domain.Gate;

/// <summary>
/// Compares active finding counts with a project's gate policy.
/// Deliberately small: the counts are computed by the caller (the seed today, the ingestion pipeline in Phase 5).
/// </summary>
public static class QualityGateEvaluator
{
    /// <summary>Evaluates <paramref name="activeCounts"/> against <paramref name="policy"/>.</summary>
    public static GateEvaluation Evaluate(SeverityCounts activeCounts, GatePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(activeCounts);
        ArgumentNullException.ThrowIfNull(policy);

        var reasons = new List<string>();
        Check("critical", activeCounts.Critical, policy.MaxCritical, reasons);
        Check("high", activeCounts.High, policy.MaxHigh, reasons);
        Check("medium", activeCounts.Medium, policy.MaxMedium, reasons);

        var result = reasons.Count == 0 ? GateResult.Passed : GateResult.Failed;
        return new GateEvaluation(result, reasons, policy, activeCounts);
    }

    private static void Check(string label, int value, int? max, List<string> reasons)
    {
        if (max is { } limit && value > limit)
        {
            reasons.Add(string.Create(CultureInfo.InvariantCulture, $"{value} {label} findings (max {limit})"));
        }
    }
}
