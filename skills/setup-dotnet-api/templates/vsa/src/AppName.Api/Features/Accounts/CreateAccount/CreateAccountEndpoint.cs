// @file-module:auth
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Features.Accounts.CreateAccount;

public sealed class CreateAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAccountsGroup()
            .MapPost("", CreateAccountAsync)
            .AllowAnonymous()
            .Produces<AccountResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Create account")
            .WithDescription("Creates a new account.");
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateAccountRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAccountCommand(request.Username, request.EmailAddress, request.Password),
            cancellationToken);

        return result.Match(
            account => Results.Created($"/v1/accounts/me", account),
            error => error.ToErrorResponse());
    }
}
