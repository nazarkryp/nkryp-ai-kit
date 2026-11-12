// @file-module:permissions
using FluentValidation;
using AppName.Application.Accounts;

namespace AppName.Application.Roles.Commands.UpdateRole;

internal sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
