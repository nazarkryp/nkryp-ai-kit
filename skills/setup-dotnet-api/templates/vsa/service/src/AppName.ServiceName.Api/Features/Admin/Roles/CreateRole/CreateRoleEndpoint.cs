// @file-module:permissions
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Roles.CreateRole;

public sealed class CreateRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminRolesGroup()
            .MapPost(string.Empty, CreateRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Create role")
            .WithDescription("Creates a role with a unique name and a set of grantable permissions.");
    }

    private static async Task<IResult> CreateRoleAsync(
        SaveRoleRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateRoleCommand(context.User.GetUser().UserId, request.Name, request.Permissions),
            cancellationToken);

        return result.Match(
            role => Results.Created($"/v1/admin/roles/{role.Id}", role),
            error => error.ToErrorResponse());
    }
}
