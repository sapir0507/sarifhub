using SarifHub.Application.Common;

namespace SarifHub.Application.Trends;

/// <summary>Use case: a project's trend. <c>null</c> when the project is not visible to the caller.</summary>
public sealed class GetTrends(ProjectReadAccess access, ITrendQuery query)
{
    /// <summary>Runs the use case.</summary>
    public async Task<IReadOnlyList<TrendPointDto>?> ExecuteAsync(Guid projectId, CancellationToken cancellationToken) =>
        await access.CanReadAsync(projectId, cancellationToken).ConfigureAwait(false)
            ? await query.GetAsync(projectId, cancellationToken).ConfigureAwait(false)
            : null;
}
