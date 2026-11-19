// @file-module:permissions
namespace AppName.ServiceName.Api.Contracts.Roles;

/// <param name="IsAdmin">True for the Admin role. Nobody can change or delete it.</param>
/// <param name="Permissions">The permission codes of the role. The Admin role has every code.</param>
/// <param name="AccountCount">How many accounts have the role. A role that an account has cannot be deleted.</param>
public sealed record RoleResponse(
    Guid Id,
    string Name,
    bool IsAdmin,
    IReadOnlyList<string> Permissions,
    int AccountCount);

/// <summary>The body of a role create and of a role update.</summary>
/// <param name="Permissions">Every permission of the role. An update replaces the current ones.</param>
public sealed record SaveRoleRequest(string Name, IReadOnlyList<string> Permissions);

/// <param name="IsGrantable">False for an admin-only permission, which no role or grant can have.</param>
public sealed record PermissionDefinitionResponse(string Code, string Description, bool IsGrantable);
