using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SarifHub.Application.Common;
using SarifHub.Application.Findings;

namespace SarifHub.Api.Controllers;

/// <summary>Findings of a project. Triage (a write) arrives in Phase 6.</summary>
[Route("api/projects/{projectId}/findings")]
public sealed class FindingsController(SearchFindings searchFindings, GetFinding getFinding) : ApiControllerBase
{
    /// <summary>
    /// One page of findings. Lists are comma-separated, e.g. <c>severity=Critical,High&amp;status=New,Reopened</c>.
    /// </summary>
    /// <param name="projectId">Project id.</param>
    /// <param name="page">0-based page (default 0).</param>
    /// <param name="pageSize">25 (default), 50 or 100.</param>
    /// <param name="sort">severity (default), lifecycle, triage, tool, ruleId, filePath, line, firstSeenAt, lastSeenAt.</param>
    /// <param name="dir">asc or desc (default).</param>
    /// <param name="severity">Critical, High, Medium, Low.</param>
    /// <param name="status">Lifecycle: New, Existing, Reopened, Resolved.</param>
    /// <param name="triage">Untriaged, Confirmed, FalsePositive, AcceptedRisk.</param>
    /// <param name="tool">Tool names, e.g. CodeQL.</param>
    /// <param name="q">Contains-search over rule id, rule name, file path and message (max 200 characters).</param>
    /// <param name="scan">Only findings present in or resolved by this scan number; lifecycle is the one in that scan.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet]
    [ProducesResponseType<PagedResult<FindingListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PagedResult<FindingListItemDto>>> List(
        Guid projectId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? dir,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        [FromQuery] string? triage,
        [FromQuery] string? tool,
        [FromQuery] string? q,
        [FromQuery] int? scan,
        CancellationToken cancellationToken)
    {
        var query = FindingsQueryParser.Parse(
            new FindingsQueryInput(page, pageSize, sort, dir, severity, status, triage, tool, q, scan),
            out var errors);
        if (query is null)
        {
            foreach (var (parameter, messages) in errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(parameter, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        return await searchFindings.ExecuteAsync(projectId, query, cancellationToken) is { } result
            ? result
            : NotFoundProblem(scan is null ? "Project not found." : "Project or scan not found.");
    }

    /// <summary>One finding with occurrences and triage history. The <c>ETag</c> header carries its version.</summary>
    [HttpGet("{findingId}")]
    [ProducesResponseType<FindingDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<FindingDetailDto>> Get(Guid projectId, Guid findingId, CancellationToken cancellationToken)
    {
        if (await getFinding.ExecuteAsync(projectId, findingId, cancellationToken) is not { } result)
        {
            return NotFoundProblem("Finding not found.");
        }

        // The version is what triage will send back in If-Match (Phase 6) to detect concurrent decisions.
        Response.Headers.ETag = $"\"{result.Version.ToString(CultureInfo.InvariantCulture)}\"";
        return result.Finding;
    }
}
