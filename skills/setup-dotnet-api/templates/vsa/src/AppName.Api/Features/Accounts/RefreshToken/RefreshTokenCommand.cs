// @file-module:auth
using AppName.Api.Common.Errors;
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Security.Abstractions;
using AppName.Api.Infrastructure.Security.Tokens;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using MediatR;
using DomainRefreshToken = AppName.Domain.Accounts.RefreshToken;

namespace AppName.Api.Features.Accounts.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<TokenResponse>>;

public sealed class RefreshTokenCommandHandler(
    IAccountRepository accountRepository,
    IRefreshTokenRepository refreshTokenRepository,
    // @module:permissions
    AccountAccessResolver accountAccessResolver,
    // @end
    ITokenProvider tokenProvider)
    : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await refreshTokenRepository.GetByHashAsync(
            RefreshTokenHash.Compute(request.RefreshToken),
            cancellationToken);

        if (existing is null)
        {
            return BusinessErrors.Accounts.InvalidRefreshToken;
        }

        if (existing.ExpiresOn < DateTime.UtcNow)
        {
            await refreshTokenRepository.DeleteAsync(existing.TokenHash, cancellationToken);
            return BusinessErrors.Accounts.RefreshTokenExpired;
        }

        var account = await accountRepository.GetByIdAsync(existing.AccountId, cancellationToken);
        if (account is null)
        {
            return BusinessErrors.Accounts.InvalidRefreshToken;
        }
        // @module:permissions

        // The role and the grants are read again, so a change applies at the next refresh.
        var access = await accountAccessResolver.ResolveAsync(account, cancellationToken);
        // @end

        var token = tokenProvider.GenerateToken(new CreateTokenParameters
        {
            UserId = account.Id,
            Username = account.Username,
            // @module:permissions
            IsAdmin = access.Permissions.IsAdmin,
            Permissions = access.Permissions.Codes,
            // @end
            EmailAddress = account.EmailAddress
        });

        var newRefreshToken = new DomainRefreshToken
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            TokenHash = RefreshTokenHash.Compute(token.RefreshToken),
            ExpiresOn = token.RefreshTokenExpiresOn
        };

        // The delete of the old token and the insert of the new one are one transaction. Two refreshes
        // with one token both read it, but only one rotates it. An end of sessions that deletes the old
        // token first leaves no new token behind.
        if (!await refreshTokenRepository.RotateAsync(existing.TokenHash, newRefreshToken, cancellationToken))
        {
            return BusinessErrors.Accounts.InvalidRefreshToken;
        }

        return new TokenResponse
        {
            TokenType = token.TokenType,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresIn = token.ExpiresIn,
            // @module:permissions
            IsAdmin = access.Permissions.IsAdmin,
            Permissions = access.Permissions.Codes,
            // @end
            ExpiresOn = token.AccessTokenExpiresOn
        };
    }
}
