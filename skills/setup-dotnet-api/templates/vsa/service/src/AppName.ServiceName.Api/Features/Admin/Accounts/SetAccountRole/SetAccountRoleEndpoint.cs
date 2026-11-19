// @file-module:permissions
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountRole;

public sealed class SetAccountRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapPut("{accountId:guid}/role", SetRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Set account role")
            .WithDescription("Gives the account a new role, or no role. An admin cannot change their own role. The last admin cannot lose the Admin role. The change reaches the account at its next token refresh.");
    }

    private static async Task<IResult> SetRoleAsync(
        Guid accountId,
        SetAccountRoleRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetAccountRoleCommand(context.User.GetUser().UserId, accountId, request.RoleId),
            cancellationToken);

        return result.ToResponse();
    }
}
