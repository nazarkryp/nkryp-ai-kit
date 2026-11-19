// @file-module:permissions
namespace AppName.ServiceName.Domain.Accounts;

/// <summary>An account as the admin sees it: what it may do, and its open setup code.</summary>
/// <param name="SetupCodeExpiresAt">Null when the account has no open setup code.</param>
public sealed record ManagedAccount(AccountAccess Access, DateTime? SetupCodeExpiresAt);
