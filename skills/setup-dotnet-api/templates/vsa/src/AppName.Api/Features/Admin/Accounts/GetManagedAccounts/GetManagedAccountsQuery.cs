// @file-module:permissions
using AppName.Api.Common;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Admin.Accounts.Shared;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Api.Features.Admin.Accounts.GetManagedAccounts;

/// <summary>Every account, ordered by username. A family server has few accounts, so the list has no pages.</summary>
public sealed record GetManagedAccountsQuery : IRequest<Result<CollectionResponse<ManagedAccountResponse>>>;

public sealed class GetManagedAccountsQueryHandler(
    ManagedAccountLoader managedAccountLoader,
    IMapper<ManagedAccount, ManagedAccountResponse> mapper)
    : IRequestHandler<GetManagedAccountsQuery, Result<CollectionResponse<ManagedAccountResponse>>>
{
    public async Task<Result<CollectionResponse<ManagedAccountResponse>>> Handle(
        GetManagedAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await managedAccountLoader.LoadAllAsync(cancellationToken);

        return new CollectionResponse<ManagedAccountResponse>
        {
            Data = accounts.Select(mapper.Map).ToList(),
            TotalCount = accounts.Count
        };
    }
}
