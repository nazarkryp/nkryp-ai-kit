// @file-module:auth
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Extensions;
using MediatR;

namespace AppName.Api.Features.Accounts.SignOut;

public sealed class SignOutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapPost("signout", SignOutAsync)
            .AllowAnySignedInUser()
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Sign out")
            .WithDescription("Invalidates the refresh token and clears the cookie.");
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (context.Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            RefreshTokenCookie.Delete(context);
        }

        var tokenMetadata = context.User.GetTokenMetadata();

        var result = await sender.Send(new RevokeTokenCommand(tokenMetadata.TokenId,
            tokenMetadata.ExpiresOn,
            refreshToken), cancellationToken);

        return result.ToResponse();
    }
}
