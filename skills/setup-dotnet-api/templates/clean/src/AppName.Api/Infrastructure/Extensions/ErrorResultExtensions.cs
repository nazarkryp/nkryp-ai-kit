using AppName.Application.Errors;
using AppName.Common;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Infrastructure.Extensions;

public static class ErrorResultExtensions
{
    public static IResult ToErrorResponse(this Error error)
    {
        var problem = error.ToProblemDetails();
        
        return Results.Problem(problem);
        // return Results.Json(data: problem, statusCode: problem.Status);
    }

    private static ProblemDetails ToProblemDetails(this Error error)
    {
        var (title, statusCode) = error switch
        {
            ValidationError         => ("Validation Failed", StatusCodes.Status400BadRequest),
            NotFoundError           => ("Not Found",         StatusCodes.Status404NotFound),
            ForbiddenError          => ("Access Denied",     StatusCodes.Status403Forbidden),
            ConflictError           => ("Conflict",          StatusCodes.Status409Conflict),
            ServiceUnavailableError => ("Service Unavailable", StatusCodes.Status503ServiceUnavailable),
            _                       => ("Internal Server Error", StatusCodes.Status500InternalServerError)
        };

        var problemDetails = new ProblemDetails
        {
            Type   = error.Name,
            Detail = error.Message,
            Title  = title,
            Status = statusCode
        };

        if (error.Details is not null)
        {
            problemDetails.Extensions["details"] = error.Details;
        }

        return problemDetails;
    }
}
