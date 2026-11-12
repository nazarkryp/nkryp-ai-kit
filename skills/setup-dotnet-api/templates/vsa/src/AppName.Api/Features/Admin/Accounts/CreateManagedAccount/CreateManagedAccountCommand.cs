// @file-module:permissions
using System.Security.Cryptography;
using AppName.Api.Common.Errors;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Admin.Accounts.Shared;
using AppName.Api.Infrastructure.Security.Abstractions;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.Api.Features.Admin.Accounts.CreateManagedAccount;

/// <summary>An admin creates an account. It has no usable password until the user enters the setup code.</summary>
/// <param name="ActorId">The admin who creates the account.</param>
public sealed record CreateManagedAccountCommand(
    Guid ActorId,
    string Username,
    string EmailAddress,
    Guid? RoleId,
    IReadOnlyList<string> Permissions) : IRequest<Result<CreatedManagedAccountResponse>>;

public sealed class CreateManagedAccountCommandHandler(
    IAccountRepository accountRepository,
    IRoleRepository roleRepository,
    IAccountSetupCodeRepository setupCodeRepository,
    IAccountSetupCodeProvider setupCodeProvider,
    IPasswordManager passwordManager,
    ManagedAccountLoader managedAccountLoader,
    IMapper<ManagedAccount, ManagedAccountResponse> mapper,
    ILogger<CreateManagedAccountCommandHandler> logger)
    : IRequestHandler<CreateManagedAccountCommand, Result<CreatedManagedAccountResponse>>
{
    public async Task<Result<CreatedManagedAccountResponse>> Handle(
        CreateManagedAccountCommand request,
        CancellationToken cancellationToken)
    {
        if (await accountRepository.GetByUsernameAsync(request.Username, cancellationToken) is not null)
        {
            return BusinessErrors.Accounts.UsernameConflict;
        }

        if (request.RoleId is { } roleId && !await roleRepository.ExistsAsync(roleId, cancellationToken))
        {
            return BusinessErrors.Roles.NotFound(roleId);
        }

        var grants = request.Permissions.Distinct(StringComparer.Ordinal).ToList();

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            Username = request.Username,
            EmailAddress = request.EmailAddress,
            RoleId = request.RoleId,
            Grants = grants,
            // The hash of a random secret that nobody knows, so nobody can sign in before the setup.
            PasswordHash = passwordManager.CreatePasswordHash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
        };

        // The check above gives the usual answer. The insert decides a race between two parallel requests.
        if (!await accountRepository.TryAddAsync(account, cancellationToken))
        {
            return BusinessErrors.Accounts.UsernameConflict;
        }

        var code = setupCodeProvider.Generate();
        var expiresAt = DateTime.UtcNow + AccountSetupCode.Lifetime;
        await setupCodeRepository.SaveAsync(new AccountSetupCode(account.Id, code.Hash, expiresAt), cancellationToken);

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} created account {AccountId} with role {RoleId} and grants {Grants}",
            "AccountCreated",
            request.ActorId,
            account.Id,
            account.RoleId,
            grants);

        var created = await managedAccountLoader.LoadAsync(account.Id, cancellationToken);
        if (created is null)
        {
            return BusinessErrors.UserNotFound;
        }

        return new CreatedManagedAccountResponse(mapper.Map(created), new AccountSetupCodeResponse(code.Value, expiresAt));
    }
}
