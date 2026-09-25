namespace SarifHub.Application.Projects;

/// <summary>Read model for projects.</summary>
public interface IProjectQueries
{
    /// <summary>Projects the user is a member of, by name. Active counts exclude suppressions valid at <paramref name="now"/>.</summary>
    Task<IReadOnlyList<ProjectSummaryDto>> ListForMemberAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
