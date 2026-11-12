// @file-module:permissions
using AppName.Api.Contracts.Roles;
using AppName.Application.Common;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Application.Roles.Queries.GetPermissionCatalog;

/// <summary>Every permission in catalog order, with its description and whether it can be granted.</summary>
public sealed record GetPermissionCatalogQuery : IRequest<Result<CollectionResponse<PermissionDefinitionResponse>>>;

internal sealed class GetPermissionCatalogQueryHandler
    : IRequestHandler<GetPermissionCatalogQuery, Result<CollectionResponse<PermissionDefinitionResponse>>>
{
    public Task<Result<CollectionResponse<PermissionDefinitionResponse>>> Handle(
        GetPermissionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = Permissions.All
            .Select(x => new PermissionDefinitionResponse(x.Code, x.Description, x.IsGrantable))
            .ToList();

        var response = new CollectionResponse<PermissionDefinitionResponse>
        {
            Data = definitions,
            TotalCount = definitions.Count
        };

        return Task.FromResult(Result.Success(response));
    }
}
