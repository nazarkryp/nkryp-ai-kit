// @file-module:auth
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.ServiceName.Application.Accounts.Configurations;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Persistence.Abstractions;
using AppName.ServiceName.Security.Abstractions;
using MediatR;
using Microsoft.Extensions.Options;

namespace AppName.ServiceName.Application.Accounts.Commands.CreateAccount;

/// <summary>
/// Self-registration. The first account of an empty database is always allowed. Later accounts need
/// <c>AccountSettings:AllowUserRegistration</c>.
/// </summary>
public sealed record CreateAccountCommand(string Username, string EmailAddress, string Password)
    : IRequest<Result<AccountResponse>>;

public sealed class CreateAccountCommandHandler(
    IAccountRepository accountRepository,
    IPasswordManager passwordManager,
    IOptions<AccountSettings> accountSettings,
    // @module:permissions
    AccountAccessResolver accountAccessResolver,
    IMapper<AccountAccess, AccountResponse> mapper)
    // @end
    // @module:!permissions
    IMapper<Account, AccountResponse> mapper)
    // @end
    : IRequestHandler<CreateAccountCommand, Result<AccountResponse>>
{
    public async Task<Result<AccountResponse>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var hasAccounts = await accountRepository.AnyAsync(cancellationToken);

        if (hasAccounts && !accountSettings.Value.AllowUserRegistration)
        {
            return BusinessErrors.Accounts.RegistrationDisabled;
        }

        var existing = await accountRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
        {
            return BusinessErrors.Accounts.UsernameConflict;
        }

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            Username = request.Username,
            EmailAddress = request.EmailAddress,
            PasswordHash = passwordManager.CreatePasswordHash(request.Password)
        };

        // The check above gives the usual answer. The insert decides a race between two parallel requests.
        if (!await accountRepository.TryAddAsync(account, cancellationToken))
        {
            return BusinessErrors.Accounts.UsernameConflict;
        }
        // @module:permissions

        // A new account has no role. The first account gets the Admin role. The promotion is one atomic
        // statement, so it is safe when two sign-ups run at the same time.
        await accountRepository.EnsureAdminExistsAsync(cancellationToken);
        // @end

        var saved = await accountRepository.GetByIdAsync(account.Id, cancellationToken);
        if (saved is null)
        {
            return BusinessErrors.UserNotFound;
        }

        // @module:permissions
        return mapper.Map(await accountAccessResolver.ResolveAsync(saved, cancellationToken));
        // @end
        // @module:!permissions
        return mapper.Map(saved);
        // @end
    }
}
