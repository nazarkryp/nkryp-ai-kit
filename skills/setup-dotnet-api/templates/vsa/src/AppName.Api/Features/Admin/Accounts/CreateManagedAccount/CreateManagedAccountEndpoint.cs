// @file-module:permissions
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Security.Extensions;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Features.Admin.Accounts.CreateManagedAccount;

public sealed class CreateManagedAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminAccountsGroup()
            .MapPost(string.Empty, CreateAccountAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CreatedManagedAccountResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Create account")
            .WithDescription("Creates an account without a usable password. The response is the only place that shows the setup code. The user enters this code to set a password.");
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateManagedAccountRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateManagedAccountCommand(
            context.User.GetUser().UserId,
            request.Username,
            request.EmailAddress,
            request.RoleId,
            request.Permissions ?? []);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            created => Results.Created($"/v1/admin/accounts/{created.Account.Id}", created),
            error => error.ToErrorResponse());
    }
}
