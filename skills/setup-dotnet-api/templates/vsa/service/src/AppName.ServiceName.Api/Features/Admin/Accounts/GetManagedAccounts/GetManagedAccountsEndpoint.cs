// @file-module:permissions
using AppName.Api.Infrastructure.Common;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.GetManagedAccounts;

public sealed class GetManagedAccountsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapGet(string.Empty, GetAccountsAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CollectionResponse<ManagedAccountResponse>>()
            .WithSummary("List accounts")
            .WithDescription("Returns every account with its role, grants and effective permissions.");
    }

    private static async Task<IResult> GetAccountsAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetManagedAccountsQuery(), cancellationToken);

        return result.ToResponse();
    }
}
