// @file-module:auth
using AppName.Api.Infrastructure.Security.Abstractions;
using AppName.Api.Infrastructure.Security.Tokens;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using MediatR;

namespace AppName.Api.Features.Accounts.SignOut;

/// <summary>Ends the current session: revokes the access token and deletes the refresh token.</summary>
public sealed record RevokeTokenCommand(string TokenId, DateTime ExpiresOn, string? RefreshToken)
    : IRequest<Result>;

public sealed class RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, ITokenManager tokenManager)
    : IRequestHandler<RevokeTokenCommand, Result>
{
    public async Task<Result> Handle(
        RevokeTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await refreshTokenRepository.DeleteAsync(RefreshTokenHash.Compute(request.RefreshToken), cancellationToken);
        }

        await tokenManager.RevokeTokenAsync(request.TokenId, request.ExpiresOn, cancellationToken);

        return Result.Success();
    }
}
