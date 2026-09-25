namespace SarifHub.Domain.Projects;

/// <summary>A member's role on one project. Roles are per project, not global.</summary>
public enum ProjectRole
{
    /// <summary>Reads everything in the project.</summary>
    Viewer = 1,

    /// <summary>Viewer, plus confirming findings and uploading scans from the UI.</summary>
    Developer = 2,

    /// <summary>Developer, plus false positive and accepted risk decisions.</summary>
    SecurityLead = 3,

    /// <summary>Everything, including members, gate policy and API keys.</summary>
    Admin = 4,
}
