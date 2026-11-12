// @file-module:auth
namespace AppName.Domain.Accounts;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    /// <summary>
    /// The SHA-256 hash of the token. The client keeps the token. A copy of the database alone cannot
    /// start a session.
    /// </summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresOn { get; set; }
}
