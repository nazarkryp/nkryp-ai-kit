// @file-module:permissions
using AppName.Api.Contracts.Accounts;
using AppName.Application.Errors;
using AppName.Common;
using AppName.Domain.Accounts;
using AppName.Application.Common.Mapping;
using MediatR;

namespace AppName.Application.Accounts.Queries.GetManagedAccount;

public sealed record GetManagedAccountQuery(Guid AccountId) : IRequest<Result<ManagedAccountResponse>>;

public sealed class GetManagedAccountQueryHandler(
    ManagedAccountLoader managedAccountLoader,
    IMapper<ManagedAccount, ManagedAccountResponse> mapper)
    : IRequestHandler<GetManagedAccountQuery, Result<ManagedAccountResponse>>
{
    public async Task<Result<ManagedAccountResponse>> Handle(
        GetManagedAccountQuery request,
        CancellationToken cancellationToken)
    {
        var managed = await managedAccountLoader.LoadAsync(request.AccountId, cancellationToken);
        if (managed is null)
        {
            return BusinessErrors.UserNotFound;
        }

        return mapper.Map(managed);
    }
}
