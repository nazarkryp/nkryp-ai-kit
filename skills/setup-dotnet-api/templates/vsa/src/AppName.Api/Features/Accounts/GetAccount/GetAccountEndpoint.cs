// @file-module:auth
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Features.Accounts.GetAccount;

public sealed class GetAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapGet("me", GetAccountAsync)
            .AllowAnySignedInUser()
            .Produces<AccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Get account")
            .WithDescription("Returns the currently authenticated account.");
    }

    private static async Task<IResult> GetAccountAsync(
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var accountId = context.User.GetUser().UserId;

        var result = await sender.Send(new GetAccountQuery(accountId), cancellationToken);
        return result.ToResponse();
    }
}
