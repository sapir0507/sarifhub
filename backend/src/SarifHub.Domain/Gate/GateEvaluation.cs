using SarifHub.Domain.Common;

namespace SarifHub.Domain.Gate;

/// <summary>
/// The quality gate result stored with a scan (ADR 0008): what CI was told, with the policy and counts used.
/// </summary>
public sealed record GateEvaluation(GateResult Result, IReadOnlyList<string> Reasons, GatePolicy Policy, SeverityCounts Counts);
