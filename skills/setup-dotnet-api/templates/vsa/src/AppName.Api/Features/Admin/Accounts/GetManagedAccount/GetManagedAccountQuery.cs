// @file-module:permissions
using AppName.Api.Common.Errors;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Admin.Accounts.Shared;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Api.Features.Admin.Accounts.GetManagedAccount;

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
