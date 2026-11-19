// @file-module:permissions
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Application.Accounts.Commands.SetAccountPermissions;

/// <param name="Permissions">Every direct grant of the account. It replaces the current grants.</param>
public sealed record SetAccountPermissionsCommand(Guid ActorId, Guid AccountId, IReadOnlyList<string> Permissions)
    : IRequest<Result<ManagedAccountResponse>>;

public sealed class SetAccountPermissionsCommandHandler(
    IAccountRepository accountRepository,
    ManagedAccountLoader managedAccountLoader,
    IMapper<ManagedAccount, ManagedAccountResponse> mapper,
    ILogger<SetAccountPermissionsCommandHandler> logger)
    : IRequestHandler<SetAccountPermissionsCommand, Result<ManagedAccountResponse>>
{
    public async Task<Result<ManagedAccountResponse>> Handle(
        SetAccountPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return BusinessErrors.UserNotFound;
        }

        var grants = request.Permissions.Distinct(StringComparer.Ordinal).ToList();

        await accountRepository.ReplaceGrantsAsync(request.AccountId, grants, cancellationToken);

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} replaced the grants of account {AccountId} from {OldGrants} to {NewGrants}",
            "GrantsReplaced",
            request.ActorId,
            request.AccountId,
            account.Grants,
            grants);

        var managed = await managedAccountLoader.LoadAsync(request.AccountId, cancellationToken);
        if (managed is null)
        {
            return BusinessErrors.UserNotFound;
        }

        return mapper.Map(managed);
    }
}
