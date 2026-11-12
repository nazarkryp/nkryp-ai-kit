// @file-module:auth
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Configurations;
using AppName.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppName.Api.Features.Accounts.SignIn;

public sealed class SignInEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapPost("signin", SignInAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AccountsRoutes.CredentialsRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces<TokenResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Sign in")
            .WithDescription("Authenticates with username and password and returns an access token.");
    }

    private static async Task<IResult> SignInAsync(
        SignInRequest request,
        HttpContext context,
        ISender sender,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SignInCommand(request.Username, request.Password),
            cancellationToken);

        return result.Match(
            tokenResponse =>
            {
                RefreshTokenCookie.Append(context, tokenResponse.RefreshToken, DateTime.UtcNow.Add(jwtOptions.Value.RefreshTokenExpiresIn));
                return Results.Ok(tokenResponse);
            },
            error => error.ToErrorResponse());
    }
}
