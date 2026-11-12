// @file-module:permissions
namespace AppName.Api.Features.Admin.Roles.GetPermissionCatalog;

/// <param name="IsGrantable">False for an admin-only permission, which no role or grant can have.</param>
public sealed record PermissionDefinitionResponse(string Code, string Description, bool IsGrantable);
