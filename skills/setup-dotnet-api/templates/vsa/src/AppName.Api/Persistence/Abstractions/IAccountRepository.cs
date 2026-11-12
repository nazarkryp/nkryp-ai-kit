// @file-module:auth
using AppName.Domain.Accounts;

namespace AppName.Api.Persistence.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The account with this username. The comparison ignores case, like the unique index.</summary>
    Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new account with its role and grants. The username is unique without case: the database
    /// enforces it (a unique index), so two parallel inserts of one name cannot both succeed.
    /// <para>
    /// After the insert, only <c>TrySetRoleAsync</c> and <c>ReplaceGrantsAsync</c> change the role and the
    /// grants (when the project has them).
    /// </para>
    /// </summary>
    /// <returns>False when another account has the username. Then nothing is written.</returns>
    Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// In one transaction: writes only the password hash, deletes every refresh token of the account
    /// except the one with <paramref name="keepRefreshTokenHash"/>, and deletes the open setup code of
    /// the account when the project has setup codes.
    /// </summary>
    /// <returns>False when no account has this id. Then nothing is written.</returns>
    Task<bool> ChangePasswordAsync(
        Guid accountId,
        string passwordHash,
        string? keepRefreshTokenHash,
        CancellationToken cancellationToken = default);
    // @module:permissions

    /// <summary>Returns every account with its direct grants, ordered by username.</summary>
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the role in one guarded statement. It changes nothing when the account does not exist,
    /// when <paramref name="roleId"/> is not null and no role has it, or when the account is the last
    /// account with the Admin role and the new role is another one. The check and the update must be
    /// atomic, so two parallel requests cannot both remove an admin.
    /// </summary>
    /// <returns>False when nothing changed.</returns>
    Task<bool> TrySetRoleAsync(Guid accountId, Guid? roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces every direct grant of the account in one transaction.</summary>
    Task ReplaceGrantsAsync(
        Guid accountId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives the Admin role to the oldest account when no account has it. The check and the update must
    /// be one atomic statement, so two parallel sign-ups cannot both become the first admin.
    /// </summary>
    Task EnsureAdminExistsAsync(CancellationToken cancellationToken = default);
    // @end
}
