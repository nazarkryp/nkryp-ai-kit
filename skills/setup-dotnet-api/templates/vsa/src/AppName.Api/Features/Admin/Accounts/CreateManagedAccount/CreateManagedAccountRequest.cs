// @file-module:permissions
namespace AppName.Api.Features.Admin.Accounts.CreateManagedAccount;

/// <param name="RoleId">The role of the new account, or null for no role.</param>
/// <param name="Permissions">The permissions to grant to the new account directly.</param>
public sealed record CreateManagedAccountRequest(
    string Username,
    string EmailAddress,
    Guid? RoleId = null,
    IReadOnlyList<string>? Permissions = null);
