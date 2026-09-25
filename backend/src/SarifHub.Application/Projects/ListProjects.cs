using SarifHub.Application.Common;

namespace SarifHub.Application.Projects;

/// <summary>Use case: the caller's projects, with role, latest scan and active findings.</summary>
public sealed class ListProjects(ICurrentUser currentUser, IProjectQueries queries, TimeProvider timeProvider)
{
    /// <summary>Runs the use case.</summary>
    public async Task<IReadOnlyList<ProjectSummaryDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is not { } id)
        {
            return [];
        }

        return await queries.ListForMemberAsync(id, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
    }
}
