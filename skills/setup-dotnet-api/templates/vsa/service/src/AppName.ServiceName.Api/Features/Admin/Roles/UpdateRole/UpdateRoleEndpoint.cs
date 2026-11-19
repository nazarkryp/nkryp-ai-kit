// @file-module:permissions
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Roles.UpdateRole;

public sealed class UpdateRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminRolesGroup()
            .MapPut("{roleId:guid}", UpdateRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<RoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Update role")
            .WithDescription("Renames the role and replaces its permissions. Nobody can change the Admin role. The change reaches the accounts of the role at their next token refresh.");
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        SaveRoleRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateRoleCommand(context.User.GetUser().UserId, roleId, request.Name, request.Permissions),
            cancellationToken);

        return result.ToResponse();
    }
}
