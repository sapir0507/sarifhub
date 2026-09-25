using Microsoft.AspNetCore.Diagnostics;
using Npgsql;

namespace SarifHub.Api.Infrastructure;

/// <summary>
/// Turns "cannot reach PostgreSQL" into <c>503 Service Unavailable</c> instead of a generic 500.
/// The response never contains the exception message (it can include host names); the log does.
/// </summary>
internal sealed partial class DatabaseUnavailableExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DatabaseUnavailableExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var npgsql = exception as NpgsqlException ?? exception.InnerException as NpgsqlException;
        if (npgsql is null || npgsql is PostgresException)
        {
            return false; // PostgresException = the server answered: a bug, handled as a 500
        }

        LogDatabaseUnavailable(logger, exception);
        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The database is unavailable.",
                Detail = "Try again shortly. If this persists, check that PostgreSQL is running.",
            },
        }).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Database unavailable")]
    private static partial void LogDatabaseUnavailable(ILogger logger, Exception exception);
}
