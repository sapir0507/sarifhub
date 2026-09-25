namespace SarifHub.Application.Common;

/// <summary>
/// The single rule every project-scoped read follows: the caller must be a member of the project.
/// A project the caller cannot see is reported exactly like one that does not exist, so ids of other
/// projects cannot be probed. Phase 6 adds role requirements for write operations next to this.
/// </summary>
public sealed class ProjectReadAccess(ICurrentUser currentUser, IProjectAccess projectAccess)
{
    /// <summary>Whether the caller may read <paramref name="projectId"/>.</summary>
    public async Task<bool> CanReadAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        return userId is { } id
            && await projectAccess.GetRoleAsync(projectId, id, cancellationToken).ConfigureAwait(false) is not null;
    }
}
