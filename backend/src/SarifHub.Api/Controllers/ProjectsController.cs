using Microsoft.AspNetCore.Mvc;
using SarifHub.Application.Projects;

namespace SarifHub.Api.Controllers;

/// <summary>Projects the caller is a member of.</summary>
[Route("api/projects")]
public sealed class ProjectsController(ListProjects listProjects) : ApiControllerBase
{
    /// <summary>Lists the caller's projects with their role, latest scan and active findings.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProjectSummaryDto>> List(CancellationToken cancellationToken) =>
        await listProjects.ExecuteAsync(cancellationToken);
}
