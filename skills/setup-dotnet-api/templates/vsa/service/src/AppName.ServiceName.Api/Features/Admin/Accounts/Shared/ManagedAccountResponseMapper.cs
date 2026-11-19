// @file-module:permissions
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.Shared;

public sealed class ManagedAccountResponseMapper : IMapper<ManagedAccount, ManagedAccountResponse>
{
    public ManagedAccountResponse Map(ManagedAccount source)
    {
        var account = source.Access.Account;
        var grants = new HashSet<string>(account.Grants, StringComparer.Ordinal);

        // A stored grant that a later version removed, or an admin-only code, is not shown, as it gives nothing.
        var grantable = Permissions.All
            .Where(x => x.IsGrantable && grants.Contains(x.Code))
            .Select(x => x.Code)
            .ToList();

        return new ManagedAccountResponse(
            account.Id,
            account.Username,
            account.EmailAddress,
            source.Access.Permissions.IsAdmin,
            account.RoleId,
            source.Access.Role?.Name,
            grantable,
            source.Access.Permissions.Codes,
            source.SetupCodeExpiresAt);
    }
}
