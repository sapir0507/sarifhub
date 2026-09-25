namespace SarifHub.Domain.Common;

/// <summary>Number of findings per severity.</summary>
public sealed record SeverityCounts(int Critical, int High, int Medium, int Low)
{
    /// <summary>All counts zero.</summary>
    public static SeverityCounts Zero { get; } = new(0, 0, 0, 0);

    /// <summary>Sum of all severities.</summary>
    public int Total => Critical + High + Medium + Low;

    /// <summary>Returns a copy with one more finding of the given severity.</summary>
    public SeverityCounts Add(Severity severity) => severity switch
    {
        Severity.Critical => this with { Critical = Critical + 1 },
        Severity.High => this with { High = High + 1 },
        Severity.Medium => this with { Medium = Medium + 1 },
        Severity.Low => this with { Low = Low + 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity."),
    };

    /// <summary>Counts the severities of a sequence.</summary>
    public static SeverityCounts Of(IEnumerable<Severity> severities)
    {
        ArgumentNullException.ThrowIfNull(severities);
        return severities.Aggregate(Zero, static (counts, severity) => counts.Add(severity));
    }
}
