using SarifHub.Application.Common;

namespace SarifHub.Application.Dashboard;

/// <summary>Use case: a project's dashboard. <c>null</c> when the project is not visible to the caller.</summary>
public sealed class GetDashboard(ICurrentUser currentUser, IDashboardQuery query, TimeProvider timeProvider)
{
    /// <summary>Runs the use case.</summary>
    public async Task<ProjectDashboardDto?> ExecuteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        // Membership is part of the dashboard query itself (it returns the caller's role), so no separate check.
        var userId = await currentUser.GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        return userId is { } id
            ? await query.GetAsync(projectId, id, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false)
            : null;
    }
}
