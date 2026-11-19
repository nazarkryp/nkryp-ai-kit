// @file-module:permissions
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountPermissions;

public sealed class SetAccountPermissionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapPut("{accountId:guid}/permissions", SetPermissionsAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Set account permissions")
            .WithDescription("Replaces the permissions granted to the account directly. The change reaches the account at its next token refresh.");
    }

    private static async Task<IResult> SetPermissionsAsync(
        Guid accountId,
        SetAccountPermissionsRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetAccountPermissionsCommand(context.User.GetUser().UserId, accountId, request.Permissions),
            cancellationToken);

        return result.ToResponse();
    }
}
