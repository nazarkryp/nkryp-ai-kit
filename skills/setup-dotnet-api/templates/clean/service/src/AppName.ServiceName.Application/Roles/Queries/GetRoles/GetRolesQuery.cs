// @file-module:permissions
using AppName.ServiceName.Api.Contracts.Roles;
using AppName.ServiceName.Application.Common;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;

namespace AppName.ServiceName.Application.Roles.Queries.GetRoles;

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
