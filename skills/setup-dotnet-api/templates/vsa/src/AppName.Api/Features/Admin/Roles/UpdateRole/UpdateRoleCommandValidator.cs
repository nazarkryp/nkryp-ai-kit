// @file-module:permissions
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Features.Admin.Roles.Shared;
using FluentValidation;

namespace AppName.Api.Features.Admin.Roles.UpdateRole;

internal sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
