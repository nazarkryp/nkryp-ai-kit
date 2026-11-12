// @file-module:auth
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Features.Accounts.ChangePassword;

public sealed class ChangePasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapPut("me/password", ChangePasswordAsync)
            .AllowAnySignedInUser()
            .RequireRateLimiting(AccountsRoutes.CredentialsRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces<AccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Change password")
            .WithDescription("Changes the password of the signed-in account. The current session stays valid, and every other session ends.");
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var user = context.User.GetUser();

        // The session that changes the password keeps its refresh token. Without the cookie, every session ends.
        var keepRefreshToken = context.Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var refreshToken)
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
}
