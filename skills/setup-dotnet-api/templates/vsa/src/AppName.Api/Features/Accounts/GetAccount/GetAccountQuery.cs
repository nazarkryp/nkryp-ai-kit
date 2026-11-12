// @file-module:auth
using AppName.Api.Common.Errors;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Api.Features.Accounts.GetAccount;

public sealed record GetAccountQuery(Guid AccountId)
    : IRequest<Result<AccountResponse>>;

public sealed class GetAccountQueryHandler(
    IAccountRepository accountRepository,
    // @module:permissions
    AccountAccessResolver accountAccessResolver,
    IMapper<AccountAccess, AccountResponse> mapper)
    // @end
    // @module:!permissions
    IMapper<Account, AccountResponse> mapper)
    // @end
    : IRequestHandler<GetAccountQuery, Result<AccountResponse>>
{
    public async Task<Result<AccountResponse>> Handle(
        GetAccountQuery request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return BusinessErrors.UserNotFound;
        }

        // @module:permissions
        return mapper.Map(await accountAccessResolver.ResolveAsync(account, cancellationToken));
        // @end
        // @module:!permissions
        return mapper.Map(account);
        // @end
    }
}
