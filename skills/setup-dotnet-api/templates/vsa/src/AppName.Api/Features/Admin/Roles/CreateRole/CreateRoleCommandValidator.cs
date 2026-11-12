// @file-module:permissions
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Features.Admin.Roles.Shared;
using FluentValidation;

namespace AppName.Api.Features.Admin.Roles.CreateRole;

internal sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
