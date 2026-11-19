// @file-module:permissions
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Application.Accounts.Commands.SetAccountRole;

/// <param name="ActorId">The admin who changes the role. An admin cannot change their own role.</param>
/// <param name="RoleId">The new role, or null for no role.</param>
public sealed record SetAccountRoleCommand(Guid ActorId, Guid AccountId, Guid? RoleId)
    : IRequest<Result<ManagedAccountResponse>>;

public sealed class SetAccountRoleCommandHandler(
    IAccountRepository accountRepository,
    IRoleRepository roleRepository,
    ManagedAccountLoader managedAccountLoader,
    IMapper<ManagedAccount, ManagedAccountResponse> mapper,
    ILogger<SetAccountRoleCommandHandler> logger)
    : IRequestHandler<SetAccountRoleCommand, Result<ManagedAccountResponse>>
{
    public async Task<Result<ManagedAccountResponse>> Handle(
        SetAccountRoleCommand request,
        CancellationToken cancellationToken)
    {
        // An admin who removes their own Admin role locks themselves out of the account management.
        if (request.ActorId == request.AccountId)
        {
            return BusinessErrors.Accounts.CannotChangeOwnRole;
        }

        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return BusinessErrors.UserNotFound;
        }

        if (!await accountRepository.TrySetRoleAsync(request.AccountId, request.RoleId, cancellationToken))
        {
            // The guarded update does not tell why it changed nothing, so read again to find the cause.
            if (request.RoleId is { } roleId && !await roleRepository.ExistsAsync(roleId, cancellationToken))
            {
                return BusinessErrors.Roles.NotFound(roleId);
            }

            if (await accountRepository.GetByIdAsync(request.AccountId, cancellationToken) is null)
            {
                return BusinessErrors.UserNotFound;
            }

            return BusinessErrors.Accounts.LastAdmin;
        }

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} changed the role of account {AccountId} from {OldRoleId} to {NewRoleId}",
            "RoleAssigned",
            request.ActorId,
            request.AccountId,
            account.RoleId,
            request.RoleId);

        var managed = await managedAccountLoader.LoadAsync(request.AccountId, cancellationToken);
        if (managed is null)
        {
            return BusinessErrors.UserNotFound;
        }

        return mapper.Map(managed);
    }
}
