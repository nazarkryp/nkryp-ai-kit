// @file-module:permissions
using AppName.Api.Infrastructure.Common.Errors;
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Common.Errors;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Infrastructure.Security.Abstractions;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Api.Features.Accounts.CompleteAccountSetup;

/// <summary>
/// The user sets a password with the setup code. The code works once, and every session of the account ends.
/// </summary>
public sealed record CompleteAccountSetupCommand(string Code, string Password)
    : IRequest<Result<AccountResponse>>;

public sealed class CompleteAccountSetupCommandHandler(
    IAccountRepository accountRepository,
    IAccountSetupCodeRepository setupCodeRepository,
    IAccountSetupCodeProvider setupCodeProvider,
    IPasswordManager passwordManager,
    IValidator<AccountPassword> passwordValidator,
    AccountAccessResolver accountAccessResolver,
    IMapper<AccountAccess, AccountResponse> mapper,
    ILogger<CompleteAccountSetupCommandHandler> logger)
    : IRequestHandler<CompleteAccountSetupCommand, Result<AccountResponse>>
{
    public async Task<Result<AccountResponse>> Handle(
        CompleteAccountSetupCommand request,
        CancellationToken cancellationToken)
    {
        // Every code failure returns the same error, so a caller cannot tell an expired code from an unknown one.
        if (!setupCodeProvider.IsFormat(request.Code))
        {
            return BusinessErrors.Accounts.InvalidSetupCode;
        }

        // One time for the whole request, so the lookup and the guarded delete agree on the expiry.
        var now = DateTime.UtcNow;
        var codeHash = setupCodeProvider.Hash(request.Code);

        var setupCode = await setupCodeRepository.GetValidAsync(codeHash, now, cancellationToken);
        if (setupCode is null)
        {
            return BusinessErrors.Accounts.InvalidSetupCode;
        }

        var account = await accountRepository.GetByIdAsync(setupCode.AccountId, cancellationToken);
        if (account is null)
        {
            return BusinessErrors.Accounts.InvalidSetupCode;
        }

        // The request has no username, so the password rules that need it run here and not in the validator.
        var validation = await passwordValidator.ValidateAsync(
            new AccountPassword(request.Password, account.Username, account.EmailAddress),
            cancellationToken);

        if (!validation.IsValid)
        {
            return ValidationError.FromFailures(validation.Errors);
        }

        var completed = await setupCodeRepository.TryCompleteSetupAsync(
            account.Id,
            codeHash,
            now,
            passwordManager.CreatePasswordHash(request.Password),
            cancellationToken);

        if (!completed)
        {
            return BusinessErrors.Accounts.InvalidSetupCode;
        }

        logger.LogInformation(
            "SecurityEvent {Event}: account {AccountId} set its password with a setup code",
            "AccountSetupCompleted",
            account.Id);

        return mapper.Map(await accountAccessResolver.ResolveAsync(account, cancellationToken));
    }
}
