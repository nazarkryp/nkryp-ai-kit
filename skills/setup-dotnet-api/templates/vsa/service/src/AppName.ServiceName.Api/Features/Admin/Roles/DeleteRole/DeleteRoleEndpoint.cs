// @file-module:permissions
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Roles.DeleteRole;

public sealed class DeleteRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminRolesGroup()
            .MapDelete("{roleId:guid}", DeleteRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Delete role")
            .WithDescription("Deletes a role that no account has. Nobody can delete the Admin role.");
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid roleId,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteRoleCommand(context.User.GetUser().UserId, roleId),
            cancellationToken);

        return result.Match(
            Results.NoContent,
            error => error.ToErrorResponse());
    }
}
