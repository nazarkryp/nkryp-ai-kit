// @file-module:permissions
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.Persistence.Abstractions;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns every role with its account count: the Admin role first, then by name.</summary>
    Task<IReadOnlyList<RoleUsage>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoleUsage?> GetUsageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether another role has this name. The comparison ignores case, like the unique index.
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the role. The role name is unique without case: the database enforces it (a unique index),
    /// so two parallel inserts of one name cannot both succeed.
    /// </summary>
    /// <returns>False when another role has the name. Then nothing is written.</returns>
    Task<bool> TryAddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>Replaces the name and the permissions in one transaction.</summary>
    /// <returns>
    /// <see cref="RoleUpdateResult.NotFound"/> when no role has this id, and
    /// <see cref="RoleUpdateResult.NameTaken"/> when another role has the new name. Then nothing is written.
    /// </returns>
    Task<RoleUpdateResult> UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the role in one guarded statement, only when it is not the Admin role and no account has it.
    /// </summary>
    /// <returns>False when nothing was deleted.</returns>
    Task<bool> TryDeleteUnusedAsync(Guid id, CancellationToken cancellationToken = default);
}

public enum RoleUpdateResult
{
    Updated,
    NotFound,
    NameTaken
}
