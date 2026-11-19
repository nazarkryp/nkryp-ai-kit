// @file-module:permissions
using AppName.ServiceName.Api.Contracts.Roles;
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Application.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(Guid ActorId, string Name, IReadOnlyList<string> Permissions)
    : IRequest<Result<RoleResponse>>;

internal sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    IMapper<RoleUsage, RoleResponse> mapper,
    ILogger<CreateRoleCommandHandler> logger) : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await roleRepository.NameExistsAsync(name, null, cancellationToken))
        {
            return BusinessErrors.Roles.NameConflict;
        }

        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Permissions = request.Permissions.ToHashSet(StringComparer.Ordinal)
        };

        // The check above gives the usual answer. The insert decides a race between two parallel requests.
        if (!await roleRepository.TryAddAsync(role, cancellationToken))
        {
            return BusinessErrors.Roles.NameConflict;
        }

        logger.LogInformation(
            "SecurityEvent {Event}: actor {ActorId} created role {RoleId} '{RoleName}' with permissions [{Permissions}]",
            "RoleCreated",
            request.ActorId,
            role.Id,
            role.Name,
            string.Join(", ", role.Permissions.Order(StringComparer.Ordinal)));

        var usage = await roleRepository.GetUsageAsync(role.Id, cancellationToken);

        // Another request can delete the new role before this read.
        if (usage is null)
        {
            return BusinessErrors.Roles.NotFound(role.Id);
        }

        return mapper.Map(usage);
    }
}
