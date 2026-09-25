using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SarifHub.Domain.Common;
using SarifHub.Domain.Gate;

namespace SarifHub.Infrastructure.Persistence.Json;

/// <summary>
/// Storage shape of <c>scans.gate_evaluation</c> — an explicit contract, independent of the domain record's
/// property names, so a refactoring cannot silently change stored history.
/// <code>{"result":"Failed","policy":{"maxCritical":0,…},"counts":{"critical":5,…},"reasons":["5 critical findings (max 0)"]}</code>
/// </summary>
internal sealed record GateEvaluationJson(GateResult Result, GatePolicyJson Policy, SeverityCountsJson Counts, IReadOnlyList<string> Reasons)
{
    public static ValueConverter<GateEvaluation?, string> Converter { get; } = new(
        v => Serialize(v),
        v => Deserialize(v));

    public static ValueComparer<GateEvaluation?> Comparer { get; } = new(
        (a, b) => Serialize(a) == Serialize(b),
        v => Serialize(v).GetHashCode(StringComparison.Ordinal),
        v => v);

    public static string Serialize(GateEvaluation? gate) => gate is null
        ? "null"
        : JsonSerializer.Serialize(
            new GateEvaluationJson(
                gate.Result,
                new GatePolicyJson(gate.Policy.MaxCritical, gate.Policy.MaxHigh, gate.Policy.MaxMedium),
                new SeverityCountsJson(gate.Counts.Critical, gate.Counts.High, gate.Counts.Medium, gate.Counts.Low),
                gate.Reasons),
            StoredJson.Options);

    public static GateEvaluation? Deserialize(string json)
    {
        var stored = Parse(json);
        return new GateEvaluation(
            stored.Result,
            stored.Reasons,
            new GatePolicy(stored.Policy.MaxCritical, stored.Policy.MaxHigh, stored.Policy.MaxMedium),
            new SeverityCounts(stored.Counts.Critical, stored.Counts.High, stored.Counts.Medium, stored.Counts.Low));
    }

    public static GateEvaluationJson Parse(string json) =>
        JsonSerializer.Deserialize<GateEvaluationJson>(json, StoredJson.Options)
        ?? throw new InvalidOperationException("A stored gate evaluation is empty.");
}

/// <summary>Stored gate policy.</summary>
internal sealed record GatePolicyJson(int? MaxCritical, int? MaxHigh, int? MaxMedium);

/// <summary>Stored severity counts.</summary>
internal sealed record SeverityCountsJson(int Critical, int High, int Medium, int Low);
