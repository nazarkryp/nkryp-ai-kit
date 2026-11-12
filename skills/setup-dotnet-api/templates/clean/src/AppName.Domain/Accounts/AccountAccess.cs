// @file-module:permissions
namespace AppName.Domain.Accounts;

/// <summary>An account together with its role and what it may do.</summary>
public sealed record AccountAccess(Account Account, Role? Role, EffectivePermissions Permissions);
