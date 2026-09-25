using SarifHub.Application.Common;

namespace SarifHub.Application.Scans;

/// <summary>Use case: a project's scans, newest first. <c>null</c> when the project is not visible to the caller.</summary>
public sealed class ListScans(ProjectReadAccess access, IScanQueries queries)
{
    /// <summary>Runs the use case.</summary>
    public async Task<IReadOnlyList<ScanSummaryDto>?> ExecuteAsync(Guid projectId, CancellationToken cancellationToken) =>
        await access.CanReadAsync(projectId, cancellationToken).ConfigureAwait(false)
            ? await queries.ListAsync(projectId, cancellationToken).ConfigureAwait(false)
            : null;
}

/// <summary>Use case: one scan. <c>null</c> when the project or the scan is not visible to the caller.</summary>
public sealed class GetScan(ProjectReadAccess access, IScanQueries queries)
{
    /// <summary>Runs the use case.</summary>
    public async Task<ScanDetailDto?> ExecuteAsync(Guid projectId, int number, CancellationToken cancellationToken) =>
        await access.CanReadAsync(projectId, cancellationToken).ConfigureAwait(false)
            ? await queries.GetAsync(projectId, number, cancellationToken).ConfigureAwait(false)
            : null;
}
