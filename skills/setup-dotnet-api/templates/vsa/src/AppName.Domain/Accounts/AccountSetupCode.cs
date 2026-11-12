// @file-module:permissions
namespace AppName.Domain.Accounts;

/// <summary>
/// The open setup code of an account. The user sets a password with it, once. Only the hash is stored.
/// </summary>
public sealed record AccountSetupCode(Guid AccountId, string CodeHash, DateTime ExpiresAt)
{
    /// <summary>How long a new setup code works.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
}
