using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace AppName.Api.Infrastructure.ExceptionHandlers;

public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.Clear();
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode  = StatusCodes.Status400BadRequest;

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext    = httpContext,
            Exception      = validationException,
            ProblemDetails = new HttpValidationProblemDetails
            {
                Type   = "InvalidRequest",
                Title  = "Validation Failed",
                Detail = "One or more validation errors occurred when processing the request",
                Status = StatusCodes.Status400BadRequest,
                Errors = errors
            }
        });
    }
}

