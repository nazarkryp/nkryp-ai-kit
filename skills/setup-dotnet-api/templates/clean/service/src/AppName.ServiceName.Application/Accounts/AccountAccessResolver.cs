// @file-module:permissions
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Persistence.Abstractions;

namespace AppName.ServiceName.Application.Accounts;

/// <summary>
/// Reads what an account may do from its role and its direct grants. Sign-in and refresh write the
/// result into the access token, so a change applies at the next refresh.
/// </summary>
public sealed class AccountAccessResolver(IAccountRepository accountRepository, IRoleRepository roleRepository)
{
    public async Task<AccountAccess> ResolveAsync(Account account, CancellationToken cancellationToken)
    {
        var role = account.RoleId is { } roleId
            ? await roleRepository.GetByIdAsync(roleId, cancellationToken)
            : null;

        return new AccountAccess(account, role, EffectivePermissions.For(role, account.Grants));
    }

    /// <summary>
    /// Whether the account may do the action now. A handler uses it for a check that depends on the
    /// request data. It reads the database, so it is never older than the access token.
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid accountId, string permission, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);

        return account is not null && (await ResolveAsync(account, cancellationToken)).Permissions.Has(permission);
    }
}
