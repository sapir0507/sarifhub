namespace SarifHub.Application.Scans;

/// <summary>Read model for scans.</summary>
public interface IScanQueries
{
    /// <summary>Completed scans of a project, newest first.</summary>
    Task<IReadOnlyList<ScanSummaryDto>> ListAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>One scan by its per-project number, or <c>null</c>.</summary>
    Task<ScanDetailDto?> GetAsync(Guid projectId, int number, CancellationToken cancellationToken);
}
