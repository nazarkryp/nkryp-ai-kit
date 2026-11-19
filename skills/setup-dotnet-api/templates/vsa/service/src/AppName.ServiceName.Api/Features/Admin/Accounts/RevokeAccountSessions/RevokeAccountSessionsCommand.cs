// @file-module:permissions
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Common.Errors;
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.RevokeAccountSessions;

/// <summary>Ends every session of the account. Its access tokens work until they expire, within one minute.</summary>
public sealed record RevokeAccountSessionsCommand(Guid ActorId, Guid AccountId)
    : IRequest<Result<ManagedAccountResponse>>;

public sealed class RevokeAccountSessionsCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ManagedAccountLoader managedAccountLoader,
    IMapper<ManagedAccount, ManagedAccountResponse> mapper,
    ILogger<RevokeAccountSessionsCommandHandler> logger)
    : IRequestHandler<RevokeAccountSessionsCommand, Result<ManagedAccountResponse>>
{
    public async Task<Result<ManagedAccountResponse>> Handle(
        RevokeAccountSessionsCommand request,
        CancellationToken cancellationToken)
    {
        // The response does not show sessions, so the account read before the delete stays correct.
        var managed = await managedAccountLoader.LoadAsync(request.AccountId, cancellationToken);
        if (managed is null)
        {
            return BusinessErrors.UserNotFound;
        }

        await refreshTokenRepository.DeleteAllForAccountAsync(request.AccountId, cancellationToken);

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} ended every session of account {AccountId}",
            "SessionsRevoked",
            request.ActorId,
            request.AccountId);

        return mapper.Map(managed);
    }
}
