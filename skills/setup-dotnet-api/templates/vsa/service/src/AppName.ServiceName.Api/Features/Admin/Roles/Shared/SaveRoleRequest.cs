// @file-module:permissions
namespace AppName.ServiceName.Api.Features.Admin.Roles.Shared;

/// <summary>The body of a role create and of a role update.</summary>
/// <param name="Permissions">Every permission of the role. An update replaces the current ones.</param>
public sealed record SaveRoleRequest(string Name, IReadOnlyList<string> Permissions);
