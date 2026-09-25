using Microsoft.AspNetCore.Mvc;

namespace SarifHub.Api.Controllers;

/// <summary>
/// Base for SarifHub controllers. Controllers are deliberately thin (ADR 0010): bind and validate the request,
/// call one Application use case, translate the result into an HTTP response.
/// </summary>
[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// 404 for anything the caller cannot see. A project the caller is not a member of is reported exactly like
    /// one that does not exist, so ids of other projects cannot be probed.
    /// </summary>
    protected ObjectResult NotFoundProblem(string title) => Problem(title: title, statusCode: StatusCodes.Status404NotFound);
}
