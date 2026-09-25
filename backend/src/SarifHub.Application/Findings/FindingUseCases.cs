using SarifHub.Application.Common;

namespace SarifHub.Application.Findings;

/// <summary>Read model for the findings grid (report-shaped: filters, sorting, paging).</summary>
public interface IFindingGridQuery
{
    /// <summary>One page of findings, or <c>null</c> when <see cref="FindingsQuery.ScanNumber"/> names no scan of the project.</summary>
    Task<PagedResult<FindingListItemDto>?> SearchAsync(Guid projectId, FindingsQuery query, CancellationToken cancellationToken);
}

/// <summary>Read model for one finding with its history.</summary>
public interface IFindingDetailQuery
{
    /// <summary>The finding, or <c>null</c> when it does not exist in the project.</summary>
    Task<VersionedFindingDetail?> GetAsync(Guid projectId, Guid findingId, CancellationToken cancellationToken);
}

/// <summary>Use case: a page of findings. <c>null</c> when the project (or the requested scan) is not visible to the caller.</summary>
public sealed class SearchFindings(ProjectReadAccess access, IFindingGridQuery query)
{
    /// <summary>Runs the use case.</summary>
    public async Task<PagedResult<FindingListItemDto>?> ExecuteAsync(Guid projectId, FindingsQuery findingsQuery, CancellationToken cancellationToken) =>
        await access.CanReadAsync(projectId, cancellationToken).ConfigureAwait(false)
            ? await query.SearchAsync(projectId, findingsQuery, cancellationToken).ConfigureAwait(false)
            : null;
}

/// <summary>Use case: one finding. <c>null</c> when the project or the finding is not visible to the caller.</summary>
public sealed class GetFinding(ProjectReadAccess access, IFindingDetailQuery query)
{
    /// <summary>Runs the use case.</summary>
    public async Task<VersionedFindingDetail?> ExecuteAsync(Guid projectId, Guid findingId, CancellationToken cancellationToken) =>
        await access.CanReadAsync(projectId, cancellationToken).ConfigureAwait(false)
            ? await query.GetAsync(projectId, findingId, cancellationToken).ConfigureAwait(false)
            : null;
}
