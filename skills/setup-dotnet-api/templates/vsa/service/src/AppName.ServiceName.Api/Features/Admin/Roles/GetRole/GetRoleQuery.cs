// @file-module:permissions
using AppName.Api.Infrastructure.Common.Mapping;
using AppName.ServiceName.Api.Common.Errors;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using MediatR;

namespace AppName.ServiceName.Api.Features.Admin.Roles.GetRole;

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
