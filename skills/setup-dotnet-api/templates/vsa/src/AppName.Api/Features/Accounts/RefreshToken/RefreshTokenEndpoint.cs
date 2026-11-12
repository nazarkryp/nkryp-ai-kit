// @file-module:auth
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Configurations;
using AppName.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppName.Api.Features.Accounts.RefreshToken;

public sealed class RefreshTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapPost("token/refresh", RefreshTokenAsync)
            .AllowAnonymous()
            .Produces<TokenResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Refresh token")
            .WithDescription("Issues a new access token using the refresh token cookie. The cookie gets a new refresh token.");
    }

    private static async Task<IResult> RefreshTokenAsync(
        HttpContext context,
        ISender sender,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var refreshToken)
            || string.IsNullOrEmpty(refreshToken))
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

        return result.Match(
            tokenResponse =>
            {
                RefreshTokenCookie.Append(context, tokenResponse.RefreshToken, DateTime.UtcNow.Add(jwtOptions.Value.RefreshTokenExpiresIn));
                return Results.Ok(tokenResponse);
            },
            error => error.ToErrorResponse());
    }
}
