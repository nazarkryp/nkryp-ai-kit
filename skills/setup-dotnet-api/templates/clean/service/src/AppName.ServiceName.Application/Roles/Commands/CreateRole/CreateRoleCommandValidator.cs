// @file-module:permissions
using FluentValidation;
using AppName.ServiceName.Application.Accounts;

namespace AppName.ServiceName.Application.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
