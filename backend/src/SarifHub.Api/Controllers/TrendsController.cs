using Microsoft.AspNetCore.Mvc;
using SarifHub.Application.Trends;

namespace SarifHub.Api.Controllers;

/// <summary>Per-scan trends of a project.</summary>
[Route("api/projects/{projectId}/trends")]
public sealed class TrendsController(GetTrends getTrends) : ApiControllerBase
{
    /// <summary>One point per scan, oldest first, read from the stored scan snapshots.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TrendPointDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<TrendPointDto>>> Get(Guid projectId, CancellationToken cancellationToken) =>
        await getTrends.ExecuteAsync(projectId, cancellationToken) is { } trend
            ? Ok(trend)
            : NotFoundProblem("Project not found.");
}
