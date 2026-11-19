// @file-module:auth
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;

namespace AppName.ServiceName.Application.Accounts.Queries.GetAccount;

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
