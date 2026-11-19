// @file-module:permissions
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Admin.Roles.GetRole;

public sealed class GetRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminRolesGroup()
            .MapGet("{roleId:guid}", GetRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<RoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Get role")
            .WithDescription("Returns one role with its permissions and the number of accounts that have it.");
    }

    private static async Task<IResult> GetRoleAsync(
        Guid roleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRoleQuery(roleId), cancellationToken);

        return result.ToResponse();
    }
}
