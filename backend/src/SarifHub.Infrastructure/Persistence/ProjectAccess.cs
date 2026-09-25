using Microsoft.EntityFrameworkCore;
using SarifHub.Application.Common;
using SarifHub.Domain.Projects;

namespace SarifHub.Infrastructure.Persistence;

/// <summary>Membership lookup (primary-key read, EF Core).</summary>
internal sealed class ProjectAccess(SarifHubDbContext db) : IProjectAccess
{
    public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken) =>
        db.ProjectMembers.AsNoTracking()
            .Where(m => m.ProjectId == projectId && m.UserId == userId)
            .Select(m => (ProjectRole?)m.Role)
            .SingleOrDefaultAsync(cancellationToken);
}
