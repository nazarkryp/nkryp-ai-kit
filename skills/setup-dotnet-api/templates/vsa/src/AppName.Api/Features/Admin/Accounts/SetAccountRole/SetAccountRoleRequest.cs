// @file-module:permissions
namespace AppName.Api.Features.Admin.Accounts.SetAccountRole;

/// <param name="RoleId">The new role, or null for no role.</param>
public sealed record SetAccountRoleRequest(Guid? RoleId);
