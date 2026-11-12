// @file-module:auth
using AppName.Api.Common.Errors;
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Security.Abstractions;
using AppName.Api.Infrastructure.Security.Tokens;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using MediatR;
using DomainRefreshToken = AppName.Domain.Accounts.RefreshToken;

namespace AppName.Api.Features.Accounts.SignIn;

public sealed record SignInCommand(string Username, string Password)
    : IRequest<Result<TokenResponse>>;

public sealed class SignInCommandHandler(
    IAccountRepository accountRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordManager passwordManager,
    // @module:permissions
    AccountAccessResolver accountAccessResolver,
    // @end
    ITokenProvider tokenProvider)
    : IRequestHandler<SignInCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(
        SignInCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (account is null || !passwordManager.VerifyPassword(request.Password, account.PasswordHash))
        {
            return BusinessErrors.Accounts.InvalidCredentials;
        }
        // @module:permissions

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

        await refreshTokenRepository.SaveAsync(new DomainRefreshToken
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            TokenHash = RefreshTokenHash.Compute(token.RefreshToken),
            ExpiresOn = token.RefreshTokenExpiresOn
        }, cancellationToken);

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
