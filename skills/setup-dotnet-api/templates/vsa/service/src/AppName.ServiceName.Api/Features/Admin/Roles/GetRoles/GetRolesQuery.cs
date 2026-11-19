// @file-module:permissions
using AppName.Api.Infrastructure.Common;
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;

namespace AppName.ServiceName.Api.Features.Admin.Roles.GetRoles;

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
