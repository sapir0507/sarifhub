using Microsoft.AspNetCore.Mvc;
using SarifHub.Application.Tools;

namespace SarifHub.Api.Controllers;

/// <summary>Tools that reported findings in a project.</summary>
[Route("api/projects/{projectId}/tools")]
public sealed class ToolsController(ListTools listTools) : ApiControllerBase
{
    /// <summary>Distinct tool names, sorted — the options of the findings tool filter.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<string>>> List(Guid projectId, CancellationToken cancellationToken) =>
        await listTools.ExecuteAsync(projectId, cancellationToken) is { } tools
            ? Ok(tools)
            : NotFoundProblem("Project not found.");
}
