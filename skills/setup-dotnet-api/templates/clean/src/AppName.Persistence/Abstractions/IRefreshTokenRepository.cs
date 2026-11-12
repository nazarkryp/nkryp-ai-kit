// @file-module:auth
using AppName.Domain.Accounts;

namespace AppName.Persistence.Abstractions;

/// <summary>
/// Stores refresh tokens by their hash (<see cref="RefreshToken.TokenHash"/>). The plain token never
/// reaches the database.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task SaveAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <returns>
    /// True when this call deleted the token. Two refreshes with one token both read it, but only one
    /// of them deletes it.
    /// </returns>
    Task<bool> DeleteAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the token with <paramref name="oldTokenHash"/> and inserts <paramref name="newToken"/> in
    /// one transaction.
    /// </summary>
    /// <returns>False when the old token was already gone. Then nothing is written.</returns>
    Task<bool> RotateAsync(string oldTokenHash, RefreshToken newToken, CancellationToken cancellationToken = default);
    // @module:permissions

    /// <summary>
    /// Ends the sessions of the account: deletes every refresh token of it. An access token that is
    /// already issued works until it expires.
    /// </summary>
    Task DeleteAllForAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    // @end
}
