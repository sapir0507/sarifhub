using Microsoft.AspNetCore.Mvc;
using SarifHub.Application.Scans;

namespace SarifHub.Api.Controllers;

/// <summary>Scans of a project. Uploading arrives in Phase 5.</summary>
[Route("api/projects/{projectId}/scans")]
public sealed class ScansController(ListScans listScans, GetScan getScan) : ApiControllerBase
{
    /// <summary>All completed scans, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ScanSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ScanSummaryDto>>> List(Guid projectId, CancellationToken cancellationToken) =>
        await listScans.ExecuteAsync(projectId, cancellationToken) is { } scans
            ? Ok(scans)
            : NotFoundProblem("Project not found.");

    /// <summary>One scan by its per-project number, with the previous scan number and severity breakdown.</summary>
    [HttpGet("{number:int}")]
    [ProducesResponseType<ScanDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ScanDetailDto>> Get(Guid projectId, int number, CancellationToken cancellationToken) =>
        await getScan.ExecuteAsync(projectId, number, cancellationToken) is { } scan
            ? scan
            : NotFoundProblem("Scan not found.");
}
