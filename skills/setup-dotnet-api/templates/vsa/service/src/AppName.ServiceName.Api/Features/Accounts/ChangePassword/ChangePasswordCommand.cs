// @file-module:auth
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Common.Errors;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Infrastructure.Security.Abstractions;
using AppName.ServiceName.Api.Infrastructure.Security.Tokens;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Api.Features.Accounts.ChangePassword;

/// <summary>The signed-in user changes their password. Their other sessions end.</summary>
/// <param name="Username">The username from the access token. The new password must not contain it.</param>
/// <param name="EmailAddress">The email address from the access token. The new password must not contain it.</param>
/// <param name="KeepRefreshToken">The refresh token of the current session, which stays valid. Null ends every session.</param>
public sealed record ChangePasswordCommand(
    Guid AccountId,
    string Username,
    string EmailAddress,
    string CurrentPassword,
    string NewPassword,
    string? KeepRefreshToken) : IRequest<Result<AccountResponse>>;

public sealed class ChangePasswordCommandHandler(
    IAccountRepository accountRepository,
    IPasswordManager passwordManager,
    // @module:permissions
    AccountAccessResolver accountAccessResolver,
    IMapper<AccountAccess, AccountResponse> mapper,
    // @end
    // @module:!permissions
    IMapper<Account, AccountResponse> mapper,
    // @end
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, Result<AccountResponse>>
{
    public async Task<Result<AccountResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return BusinessErrors.UserNotFound;
        }

        // A stolen access token alone must not be enough to take over the account.
        if (!passwordManager.VerifyPassword(request.CurrentPassword, account.PasswordHash))
        {
            return BusinessErrors.Accounts.CurrentPasswordInvalid;
        }

        if (!await accountRepository.ChangePasswordAsync(
                account.Id,
                passwordManager.CreatePasswordHash(request.NewPassword),
                request.KeepRefreshToken is null ? null : RefreshTokenHash.Compute(request.KeepRefreshToken),
                cancellationToken))
        {
            return BusinessErrors.UserNotFound;
        }

        logger.LogInformation(
            "SecurityEvent {Event}: account {AccountId} changed its password and ended its other sessions",
            "PasswordChanged",
            account.Id);

        // @module:permissions
        return mapper.Map(await accountAccessResolver.ResolveAsync(account, cancellationToken));
        // @end
        // @module:!permissions
        return mapper.Map(account);
        // @end
    }
}
