// @file-module:auth
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.Api.Infrastructure;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Application.Accounts.Commands.ChangePassword;
// @module:permissions
using AppName.ServiceName.Application.Accounts.Commands.CompleteAccountSetup;
// @end
using AppName.ServiceName.Application.Accounts.Commands.CreateAccount;
using AppName.ServiceName.Application.Accounts.Commands.RefreshToken;
using AppName.ServiceName.Application.Accounts.Commands.SignIn;
using AppName.ServiceName.Application.Accounts.Commands.SignOut;
using AppName.ServiceName.Application.Accounts.Queries.GetAccount;
using AppName.Common;
using AppName.ServiceName.Security.Configurations;
using AppName.Api.Authentication.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AppName.ServiceName.Api.Endpoints;

public sealed class AccountsEndpoints : IEndpoint
{
    /// <summary>The rate limit on the routes that check a password.</summary>
    public const string CredentialsRateLimitPolicy = "AccountCredentials";

    /// <summary>
    /// The HttpOnly cookie that holds the refresh token. It is SameSite=Strict, so the browser sends it
    /// only from a page on the same site as the API.
    /// </summary>
    private const string RefreshTokenCookie = "refresh_token";

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        var versionSet = builder.CreateDefaultApiVersionSet();

        var group = builder.MapGroup("v{v:apiVersion}/accounts")
            .WithApiVersionSet(versionSet)
            .WithTags("Accounts");

        group.MapPost("", CreateAccountAsync)
            .AllowAnonymous()
            .Produces<AccountResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Create account")
            .WithDescription("Creates a new account.");

        group.MapGet("me", GetAccountAsync)
            .AllowAnySignedInUser()
            .Produces<AccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Get account")
            .WithDescription("Returns the currently authenticated account.");

        group.MapPost("signin", SignInAsync)
            .AllowAnonymous()
            .RequireRateLimiting(CredentialsRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces<TokenResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Sign in")
            .WithDescription("Authenticates with username and password and returns an access token.");

        group.MapPost("token/refresh", RefreshTokenAsync)
            .AllowAnonymous()
            .Produces<TokenResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Refresh token")
            .WithDescription("Issues a new access token using the refresh token cookie. The cookie gets a new refresh token.");

        // @module:permissions
        group.MapPost("setup", CompleteSetupAsync)
            .AllowAnonymous()
            .RequireRateLimiting(CredentialsRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces<AccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Complete account setup")
            .WithDescription("Sets the password of the account with the one-time setup code from the admin. Every session of the account ends.");
        // @end

        group.MapPut("me/password", ChangePasswordAsync)
            .AllowAnySignedInUser()
            .RequireRateLimiting(CredentialsRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces<AccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Change password")
            .WithDescription("Changes the password of the signed-in account. The current session stays valid, and every other session ends.");

        group.MapPost("signout", SignOutAsync)
            .AllowAnySignedInUser()
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Sign out")
            .WithDescription("Invalidates the refresh token and clears the cookie.");
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateAccountRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAccountCommand(request.Username, request.EmailAddress, request.Password),
            cancellationToken);

        return result.Match(
            account => Results.Created($"/v1/accounts/me", account),
            error => error.ToErrorResponse());
    }

    private static async Task<IResult> GetAccountAsync(
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var accountId = context.User.GetUser().UserId;

        var result = await sender.Send(new GetAccountQuery(accountId), cancellationToken);
        return result.ToResponse();
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
                SetRefreshTokenCookie(context, tokenResponse.RefreshToken, DateTime.UtcNow.Add(jwtOptions.Value.RefreshTokenExpiresIn));
                return Results.Ok(tokenResponse);
            },
            error => error.ToErrorResponse());
    }

    private static async Task<IResult> RefreshTokenAsync(
        HttpContext context,
        ISender sender,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken)
            || string.IsNullOrEmpty(refreshToken))
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

        return result.Match(
            tokenResponse =>
            {
                SetRefreshTokenCookie(context, tokenResponse.RefreshToken, DateTime.UtcNow.Add(jwtOptions.Value.RefreshTokenExpiresIn));
                return Results.Ok(tokenResponse);
            },
            error => error.ToErrorResponse());
    }

    // @module:permissions
    private static async Task<IResult> CompleteSetupAsync(
        CompleteAccountSetupRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteAccountSetupCommand(request.Code, request.Password),
            cancellationToken);

        return result.ToResponse();
    }
    // @end

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var user = context.User.GetUser();

        // The session that changes the password keeps its refresh token. Without the cookie, every session ends.
        var keepRefreshToken = context.Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken)
            && !string.IsNullOrEmpty(refreshToken)
                ? refreshToken
                : null;

        var result = await sender.Send(
            new ChangePasswordCommand(
                user.UserId,
                user.Username,
                user.EmailAddress,
                request.CurrentPassword,
                request.NewPassword,
                keepRefreshToken),
            cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (context.Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            context.Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict
            });
        }

        var tokenMetadata = context.User.GetTokenMetadata();

        var result = await sender.Send(new RevokeTokenCommand(tokenMetadata.TokenId,
            tokenMetadata.ExpiresOn,
            refreshToken), cancellationToken);

        return result.ToResponse();
    }

    private static void SetRefreshTokenCookie(HttpContext context, string refreshToken, DateTime expiresOn)
    {
        context.Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expiresOn
        });
    }
}