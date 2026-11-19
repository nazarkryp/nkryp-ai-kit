using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Infrastructure.ExceptionHandlers;

/// <summary>
/// Writes an unhandled exception as a 500 ProblemDetails. Only the Development environment shows the
/// exception message, because a message can contain paths, SQL or other internal data.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        logger.LogError(exception, "Unhandled exception '{ExceptionType}': {Message}",
            exception.GetType().Name, exception.Message);

        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext    = httpContext,
            Exception      = exception,
            ProblemDetails = new ProblemDetails
            {
                Type   = nameof(HttpStatusCode.InternalServerError),
                Title  = "Internal Server Error",
                Detail = environment.IsDevelopment() ? exception.Message : "An unexpected error occurred."
            }
        });
    }
}

