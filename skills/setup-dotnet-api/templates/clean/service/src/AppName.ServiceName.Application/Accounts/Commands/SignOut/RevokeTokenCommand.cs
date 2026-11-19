// @file-module:auth
using AppName.Common;
using AppName.ServiceName.Persistence.Abstractions;
using AppName.ServiceName.Security.Abstractions;
using AppName.ServiceName.Security.Tokens;
using MediatR;

namespace AppName.ServiceName.Application.Accounts.Commands.SignOut;

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
