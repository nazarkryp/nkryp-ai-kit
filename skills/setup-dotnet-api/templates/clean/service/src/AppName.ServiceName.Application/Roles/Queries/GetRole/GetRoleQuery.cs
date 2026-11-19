// @file-module:permissions
using AppName.ServiceName.Api.Contracts.Roles;
using AppName.ServiceName.Application.Errors;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Persistence.Abstractions;
using MediatR;

namespace AppName.ServiceName.Application.Roles.Queries.GetRole;

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
