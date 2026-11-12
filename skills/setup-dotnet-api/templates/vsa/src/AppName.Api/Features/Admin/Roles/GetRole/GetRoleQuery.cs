// @file-module:permissions
using AppName.Api.Common.Errors;
using AppName.Api.Common.Mapping;
using AppName.Api.Features.Admin.Roles.Shared;
using AppName.Api.Persistence.Abstractions;
using AppName.Common;
using AppName.Domain.Accounts;
using MediatR;

namespace AppName.Api.Features.Admin.Roles.GetRole;

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
