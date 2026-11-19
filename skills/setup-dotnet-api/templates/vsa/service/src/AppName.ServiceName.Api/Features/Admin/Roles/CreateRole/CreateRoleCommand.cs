// @file-module:permissions
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Common.Errors;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppName.ServiceName.Api.Features.Admin.Roles.CreateRole;

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
