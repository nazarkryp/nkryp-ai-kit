// @file-module:permissions
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.Features.Admin.Roles.Shared;

public sealed class RoleResponseMapper : IMapper<RoleUsage, RoleResponse>
{
    public RoleResponse Map(RoleUsage source)
    {
        var role = source.Role;

        // The Admin role stores no codes but has every permission. A stored code that is unknown or
        // admin-only gives nothing, so the response does not show it.
        var permissions = Permissions.All
            .Where(definition => role.IsAdmin || (definition.IsGrantable && role.Permissions.Contains(definition.Code)))
            .Select(definition => definition.Code)
            .ToList();

        return new RoleResponse(role.Id, role.Name, role.IsAdmin, permissions, source.AccountCount);
    }
}
