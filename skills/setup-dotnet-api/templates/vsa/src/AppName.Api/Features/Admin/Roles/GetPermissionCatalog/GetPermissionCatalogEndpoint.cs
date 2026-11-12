// @file-module:permissions
using AppName.Api.Common;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Api.Features.Admin.Roles.GetPermissionCatalog;

public sealed class GetPermissionCatalogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapAdminPermissionsGroup()
            .MapGet(string.Empty, GetPermissionCatalogAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CollectionResponse<PermissionDefinitionResponse>>()
            .WithSummary("List permissions")
            .WithDescription("Returns every permission code with its description, and if a role or a grant can have it.");
    }

    private static async Task<IResult> GetPermissionCatalogAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPermissionCatalogQuery(), cancellationToken);

        return result.ToResponse();
    }
}
