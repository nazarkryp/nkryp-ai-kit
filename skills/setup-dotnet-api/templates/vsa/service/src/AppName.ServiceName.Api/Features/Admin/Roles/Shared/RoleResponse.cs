// @file-module:permissions
namespace AppName.ServiceName.Api.Features.Admin.Roles.Shared;

/// <param name="IsAdmin">True for the Admin role. Nobody can change or delete it.</param>
/// <param name="Permissions">The permission codes of the role. The Admin role has every code.</param>
/// <param name="AccountCount">How many accounts have the role. A role that an account has cannot be deleted.</param>
public sealed record RoleResponse(
    Guid Id,
    string Name,
    bool IsAdmin,
    IReadOnlyList<string> Permissions,
    int AccountCount);
