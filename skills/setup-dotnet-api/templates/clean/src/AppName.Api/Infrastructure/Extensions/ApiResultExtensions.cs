using AppName.Common;

namespace AppName.Api.Infrastructure.Extensions;

public static class ApiResultExtensions
{
    public static IResult ToResponse<TValue>(this Result<TValue> result) =>
        result.Match(Results.Ok, error => error.ToErrorResponse());

    public static IResult ToResponse(this Result result) =>
        result.Match(Results.NoContent, error => error.ToErrorResponse());
}

