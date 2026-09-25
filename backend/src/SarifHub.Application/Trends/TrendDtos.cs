namespace SarifHub.Application.Trends;

/// <summary>
/// One point per scan (<c>TrendPoint</c>): present findings by severity, the diff, and results per tool.
/// Read from the immutable scan snapshots, so history never changes when findings are triaged later.
/// </summary>
public sealed record TrendPointDto(
    int ScanNumber,
    DateTimeOffset Date,
    int Critical,
    int High,
    int Medium,
    int Low,
    int NewCount,
    int ResolvedCount,
    int ReopenedCount,
    IReadOnlyDictionary<string, int> ByTool);

/// <summary>Read model for trends.</summary>
public interface ITrendQuery
{
    /// <summary>Trend points in scan order.</summary>
    Task<IReadOnlyList<TrendPointDto>> GetAsync(Guid projectId, CancellationToken cancellationToken);
}
