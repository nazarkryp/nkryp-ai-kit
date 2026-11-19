// @file-module:auth
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Common.Errors;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;

namespace AppName.ServiceName.Api.Features.Accounts.GetAccount;

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
