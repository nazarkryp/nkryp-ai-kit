// @file-module:permissions
using AppName.Api.Common;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Admin.Roles.Shared;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Api.Features.Admin.Roles.GetRoles;

/// <summary>Every role: the Admin role first, then by name. The list is short, so it has no pages.</summary>
public sealed record GetRolesQuery : IRequest<Result<CollectionResponse<RoleResponse>>>;

internal sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    IMapper<RoleUsage, RoleResponse> mapper) : IRequestHandler<GetRolesQuery, Result<CollectionResponse<RoleResponse>>>
{
    public async Task<Result<CollectionResponse<RoleResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.ListAsync(cancellationToken);

        return new CollectionResponse<RoleResponse>
        {
            Data = roles.Select(mapper.Map).ToList(),
            TotalCount = roles.Count
        };
    }
}
