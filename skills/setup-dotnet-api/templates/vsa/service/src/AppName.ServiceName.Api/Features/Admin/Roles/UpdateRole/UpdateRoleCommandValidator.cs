// @file-module:permissions
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using FluentValidation;

namespace AppName.ServiceName.Api.Features.Admin.Roles.UpdateRole;

internal sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
