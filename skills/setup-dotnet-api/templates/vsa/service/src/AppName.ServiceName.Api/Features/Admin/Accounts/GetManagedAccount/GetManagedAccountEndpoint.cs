// @file-module:permissions
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.GetManagedAccount;

public sealed class GetManagedAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapGet("{accountId:guid}", GetAccountAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Get account")
            .WithDescription("Returns one account with its role, grants and effective permissions.");
    }

    private static async Task<IResult> GetAccountAsync(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetManagedAccountQuery(accountId), cancellationToken);

        return result.ToResponse();
    }
}
