// @file-module:permissions
using AppName.Api.Features.Admin.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Extensions;
using AppName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Features.Admin.Accounts.IssueAccountSetupCode;

public sealed class IssueAccountSetupCodeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapPost("{accountId:guid}/setup-code", IssueSetupCodeAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<AccountSetupCodeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Issue account setup code")
            .WithDescription("Gives the account a new setup code. The new code replaces the old one. The response is the only place that shows the code.");
    }

    private static async Task<IResult> IssueSetupCodeAsync(
        Guid accountId,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new IssueAccountSetupCodeCommand(context.User.GetUser().UserId, accountId),
            cancellationToken);

        return result.ToResponse();
    }
}
