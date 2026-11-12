// @file-module:auth
using AppName.Api.Contracts.Accounts;
using AppName.Application.Common.Mapping;
using AppName.Domain.Accounts;

namespace AppName.Application.Accounts.Mapping;

// @module:permissions
public sealed class AccountResponseMapper : IMapper<AccountAccess, AccountResponse>
{
    public AccountResponse Map(AccountAccess source) => new()
    {
        Id = source.Account.Id,
        Username = source.Account.Username,
        EmailAddress = source.Account.EmailAddress,
        IsAdmin = source.Permissions.IsAdmin,
        RoleId = source.Role?.Id,
        RoleName = source.Role?.Name,
        Permissions = source.Permissions.Codes
    };
}
// @end
// @module:!permissions
public sealed class AccountResponseMapper : IMapper<Account, AccountResponse>
{
    public AccountResponse Map(Account source) => new()
    {
        Id = source.Id,
        Username = source.Username,
        EmailAddress = source.EmailAddress
    };
}
// @end
