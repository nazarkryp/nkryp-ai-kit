// @file-module:permissions
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.Shared;

/// <summary>Reads accounts as the admin sees them: with their role, their permissions and their open setup code.</summary>
public sealed class ManagedAccountLoader(
    IAccountRepository accountRepository,
    AccountAccessResolver accountAccessResolver,
    IAccountSetupCodeRepository setupCodeRepository)
{
    public async Task<ManagedAccount?> LoadAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var access = await accountAccessResolver.ResolveAsync(account, cancellationToken);
        var expiries = await setupCodeRepository.GetOpenExpiriesAsync(DateTime.UtcNow, cancellationToken);

        return new ManagedAccount(access, expiries.TryGetValue(accountId, out var expiresAt) ? expiresAt : null);
    }

    public async Task<IReadOnlyList<ManagedAccount>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var accounts = await accountRepository.ListAsync(cancellationToken);
        var expiries = await setupCodeRepository.GetOpenExpiriesAsync(DateTime.UtcNow, cancellationToken);

        // Many accounts share a role, so each role is read once and reused for the other accounts that have it.
        var roles = new Dictionary<Guid, Role?>();
        var result = new List<ManagedAccount>(accounts.Count);

        foreach (var account in accounts)
        {
            AccountAccess access;

            if (account.RoleId is { } roleId && roles.TryGetValue(roleId, out var role))
            {
                access = new AccountAccess(account, role, EffectivePermissions.For(role, account.Grants));
            }
            else
            {
                access = await accountAccessResolver.ResolveAsync(account, cancellationToken);

                if (account.RoleId is { } resolvedRoleId)
                {
                    roles[resolvedRoleId] = access.Role;
                }
            }

            result.Add(new ManagedAccount(access, expiries.TryGetValue(account.Id, out var expiresAt) ? expiresAt : null));
        }

        return result;
    }
}
