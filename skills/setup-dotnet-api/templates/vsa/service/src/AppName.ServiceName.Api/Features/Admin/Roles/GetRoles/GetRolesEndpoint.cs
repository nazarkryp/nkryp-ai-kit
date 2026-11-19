// @file-module:permissions
using AppName.Api.Infrastructure.Common;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Domain.Accounts;
using MediatR;

namespace AppName.ServiceName.Api.Features.Admin.Roles.GetRoles;

public sealed class GetRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminRolesGroup()
            .MapGet(string.Empty, GetRolesAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CollectionResponse<RoleResponse>>()
            .WithSummary("List roles")
            .WithDescription("Returns every role with its permissions and the number of accounts that have it.");
    }

    private static async Task<IResult> GetRolesAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRolesQuery(), cancellationToken);

        return result.ToResponse();
    }
}
