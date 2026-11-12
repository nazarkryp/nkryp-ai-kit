// @file-module:permissions
using AppName.Api.Common.Errors;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Admin.Accounts.Shared;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.Api.Features.Admin.Accounts.SetAccountPermissions;

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
