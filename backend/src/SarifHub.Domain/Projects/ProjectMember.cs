namespace SarifHub.Domain.Projects;

/// <summary>A user's membership of a project, with the role they have there.</summary>
public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    internal ProjectMember(Guid projectId, Guid userId, ProjectRole role, DateTimeOffset addedAt)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        AddedAt = addedAt;
    }

    /// <summary>The project.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>The member.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The member's role on this project.</summary>
    public ProjectRole Role { get; private set; }

    /// <summary>When the user was added (UTC).</summary>
    public DateTimeOffset AddedAt { get; private set; }
}
