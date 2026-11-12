// @file-module:permissions
using FluentValidation;
using AppName.Application.Accounts;

namespace AppName.Application.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
