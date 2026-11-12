// @file-module:permissions
using AppName.Api.Contracts.Accounts;
using AppName.Application.Errors;
using AppName.Common;
using AppName.Domain.Accounts;
using AppName.Persistence.Abstractions;
using AppName.Security.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.Application.Accounts.Commands.IssueAccountSetupCode;

/// <summary>
/// Gives the account a new setup code, for example when the user forgot the password or the old code
/// expired. The new code replaces the old one. The current password works until the user uses the code.
/// </summary>
public sealed record IssueAccountSetupCodeCommand(Guid ActorId, Guid AccountId)
    : IRequest<Result<AccountSetupCodeResponse>>;

public sealed class IssueAccountSetupCodeCommandHandler(
    IAccountRepository accountRepository,
    IAccountSetupCodeRepository setupCodeRepository,
    IAccountSetupCodeProvider setupCodeProvider,
    ILogger<IssueAccountSetupCodeCommandHandler> logger)
    : IRequestHandler<IssueAccountSetupCodeCommand, Result<AccountSetupCodeResponse>>
{
    public async Task<Result<AccountSetupCodeResponse>> Handle(
        IssueAccountSetupCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (await accountRepository.GetByIdAsync(request.AccountId, cancellationToken) is null)
        {
            return BusinessErrors.UserNotFound;
        }

        var code = setupCodeProvider.Generate();
        var expiresAt = DateTime.UtcNow + AccountSetupCode.Lifetime;
        await setupCodeRepository.SaveAsync(new AccountSetupCode(request.AccountId, code.Hash, expiresAt), cancellationToken);

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} issued a setup code for account {AccountId} that expires at {ExpiresAt}",
            "SetupCodeIssued",
            request.ActorId,
            request.AccountId,
            expiresAt);

        return new AccountSetupCodeResponse(code.Value, expiresAt);
    }
}
