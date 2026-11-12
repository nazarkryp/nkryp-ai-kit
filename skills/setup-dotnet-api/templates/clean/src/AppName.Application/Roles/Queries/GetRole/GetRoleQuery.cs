// @file-module:permissions
using AppName.Api.Contracts.Roles;
using AppName.Application.Errors;
using AppName.Common;
using AppName.Domain.Accounts;
using AppName.Application.Common.Mapping;
using AppName.Persistence.Abstractions;
using MediatR;

namespace AppName.Application.Roles.Queries.GetRole;

public sealed record GetRoleQuery(Guid RoleId) : IRequest<Result<RoleResponse>>;

internal sealed class GetRoleQueryHandler(
    IRoleRepository roleRepository,
    IMapper<RoleUsage, RoleResponse> mapper) : IRequestHandler<GetRoleQuery, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var usage = await roleRepository.GetUsageAsync(request.RoleId, cancellationToken);

        if (usage is null)
        {
            return BusinessErrors.Roles.NotFound(request.RoleId);
        }

        return mapper.Map(usage);
    }
}
