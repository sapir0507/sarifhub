using Microsoft.AspNetCore.Mvc;
using SarifHub.Application.Dashboard;

namespace SarifHub.Api.Controllers;

/// <summary>The project dashboard.</summary>
[Route("api/projects/{projectId}/dashboard")]
public sealed class DashboardController(GetDashboard getDashboard) : ApiControllerBase
{
    /// <summary>Everything the dashboard shows, in one response.</summary>
    [HttpGet]
    [ProducesResponseType<ProjectDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ProjectDashboardDto>> Get(Guid projectId, CancellationToken cancellationToken) =>
        await getDashboard.ExecuteAsync(projectId, cancellationToken) is { } dashboard
            ? dashboard
            : NotFoundProblem("Project not found.");
}
