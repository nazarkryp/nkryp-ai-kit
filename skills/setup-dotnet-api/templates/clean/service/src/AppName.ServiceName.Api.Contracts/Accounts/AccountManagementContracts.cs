// @file-module:permissions
namespace AppName.ServiceName.Api.Contracts.Accounts;

/// <param name="RoleId">The role of the new account, or null for no role.</param>
/// <param name="Permissions">The permissions to grant to the new account directly.</param>
public sealed record CreateManagedAccountRequest(
    string Username,
    string EmailAddress,
    Guid? RoleId = null,
    IReadOnlyList<string>? Permissions = null);

/// <param name="Account">The new account. It has no usable password yet.</param>
/// <param name="SetupCode">The code that the user enters to set a password. The server shows it only once.</param>
public sealed record CreatedManagedAccountResponse(ManagedAccountResponse Account, AccountSetupCodeResponse SetupCode);

/// <param name="Code">The one-time setup code. The server stores only its hash, so it shows the code only once.</param>
public sealed record AccountSetupCodeResponse(string Code, DateTime ExpiresAt);

/// <param name="RoleId">The new role, or null for no role.</param>
public sealed record SetAccountRoleRequest(Guid? RoleId);

/// <param name="Permissions">Every permission granted to the account directly. It replaces the current grants.</param>
public sealed record SetAccountPermissionsRequest(IReadOnlyList<string> Permissions);

public sealed record CompleteAccountSetupRequest(string Code, string Password);
