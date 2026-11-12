// @file-module:permissions
using AppName.Api.Contracts.Roles;
using AppName.Application.Errors;
using AppName.Common;
using AppName.Domain.Accounts;
using AppName.Application.Common.Mapping;
using AppName.Persistence.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.Application.Roles.Commands.UpdateRole;

/// <param name="Permissions">Every permission of the role. It replaces the current ones.</param>
public sealed record UpdateRoleCommand(Guid ActorId, Guid RoleId, string Name, IReadOnlyList<string> Permissions)
    : IRequest<Result<RoleResponse>>;

internal sealed class UpdateRoleCommandHandler(
    IRoleRepository roleRepository,
    IMapper<RoleUsage, RoleResponse> mapper,
    ILogger<UpdateRoleCommandHandler> logger) : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.RoleId == Role.AdminId)
        {
            return BusinessErrors.Roles.AdminRoleLocked;
        }

        // The old values are read only for the audit line.
        var current = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        if (current is null)
        {
            return BusinessErrors.Roles.NotFound(request.RoleId);
        }

        var name = request.Name.Trim();

        if (await roleRepository.NameExistsAsync(name, request.RoleId, cancellationToken))
        {
            return BusinessErrors.Roles.NameConflict;
        }

        var role = new Role
        {
            Id = request.RoleId,
            Name = name,
            Permissions = request.Permissions.ToHashSet(StringComparer.Ordinal)
        };

        // Another request can delete the role, or take the name, after the checks above.
        switch (await roleRepository.UpdateAsync(role, cancellationToken))
        {
            case RoleUpdateResult.NotFound:
                return BusinessErrors.Roles.NotFound(request.RoleId);
            case RoleUpdateResult.NameTaken:
                return BusinessErrors.Roles.NameConflict;
        }

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} updated role {RoleId} from '{OldName}' [{OldPermissions}] to '{NewName}' [{NewPermissions}]",
            "RoleUpdated",
            request.ActorId,
            role.Id,
            current.Name,
            string.Join(", ", current.Permissions.Order(StringComparer.Ordinal)),
            role.Name,
            string.Join(", ", role.Permissions.Order(StringComparer.Ordinal)));

        var usage = await roleRepository.GetUsageAsync(role.Id, cancellationToken);

        if (usage is null)
        {
            return BusinessErrors.Roles.NotFound(role.Id);
        }

        return mapper.Map(usage);
    }
}
