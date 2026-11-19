// @file-module:permissions
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Accounts.CompleteAccountSetup;

public sealed class CompleteAccountSetupEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapPost("setup", CompleteSetupAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AccountsRoutes.CredentialsRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces<AccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Complete account setup")
            .WithDescription("Sets the password of the account with the one-time setup code from the admin. Every session of the account ends.");
    }

    private static async Task<IResult> CompleteSetupAsync(
        CompleteAccountSetupRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteAccountSetupCommand(request.Code, request.Password),
            cancellationToken);

        return result.ToResponse();
    }
}
