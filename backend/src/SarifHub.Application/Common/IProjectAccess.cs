using SarifHub.Domain.Projects;

namespace SarifHub.Application.Common;

/// <summary>Looks up project membership.</summary>
public interface IProjectAccess
{
    /// <summary>The user's role on the project, or <c>null</c> when the user is not a member (or the project does not exist).</summary>
    Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
}
