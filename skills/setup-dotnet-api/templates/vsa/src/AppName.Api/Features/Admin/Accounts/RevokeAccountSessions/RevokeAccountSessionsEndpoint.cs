// @file-module:permissions
using AppName.Api.Features.Admin.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Extensions;
using AppName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Features.Admin.Accounts.RevokeAccountSessions;

public sealed class RevokeAccountSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapPost("{accountId:guid}/sessions/revoke", RevokeSessionsAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Revoke account sessions")
            .WithDescription("Ends every session of the account. Its access tokens work until they expire, within one minute.");
    }

    private static async Task<IResult> RevokeSessionsAsync(
        Guid accountId,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RevokeAccountSessionsCommand(context.User.GetUser().UserId, accountId),
            cancellationToken);

        return result.ToResponse();
    }
}
