// @file-module:permissions
namespace AppName.Api.Features.Admin.Accounts.SetAccountPermissions;

/// <param name="Permissions">Every permission granted to the account directly. It replaces the current grants.</param>
public sealed record SetAccountPermissionsRequest(IReadOnlyList<string> Permissions);
