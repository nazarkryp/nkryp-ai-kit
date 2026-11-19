// @file-module:permissions
namespace AppName.ServiceName.Api.Contracts.Accounts;

/// <summary>An account as the admin sees it in the account management.</summary>
/// <param name="RoleId">The role of the account, or null when it has none.</param>
/// <param name="Grants">The permissions granted to the account directly, on top of its role.</param>
/// <param name="Permissions">Every permission of the account: its role and its grants. An admin has every code.</param>
/// <param name="SetupCodeExpiresAt">When the open setup code expires. Null when the account has no open setup code.</param>
public sealed record ManagedAccountResponse(
    Guid Id,
    string Username,
    string EmailAddress,
    bool IsAdmin,
    Guid? RoleId,
    string? RoleName,
    IReadOnlyList<string> Grants,
    IReadOnlyList<string> Permissions,
    DateTime? SetupCodeExpiresAt);
