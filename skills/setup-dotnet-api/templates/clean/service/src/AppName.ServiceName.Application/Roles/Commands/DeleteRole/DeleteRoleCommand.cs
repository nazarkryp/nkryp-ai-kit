// @file-module:permissions
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Application.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid ActorId, Guid RoleId) : IRequest<Result>;

internal sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository,
    ILogger<DeleteRoleCommandHandler> logger) : IRequestHandler<DeleteRoleCommand, Result>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.RoleId == Role.AdminId)
        {
            return BusinessErrors.Roles.AdminRoleLocked;
        }

        if (!await roleRepository.ExistsAsync(request.RoleId, cancellationToken))
        {
            return BusinessErrors.Roles.NotFound(request.RoleId);
        }

        // The role exists, so a failed guarded delete means that an account has it, also when the
        // account got it after the check above.
        if (!await roleRepository.TryDeleteUnusedAsync(request.RoleId, cancellationToken))
        {
            return BusinessErrors.Roles.InUse;
        }

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} deleted role {RoleId}",
            "RoleDeleted",
            request.ActorId,
            request.RoleId);

        return Result.Success();
    }
}
