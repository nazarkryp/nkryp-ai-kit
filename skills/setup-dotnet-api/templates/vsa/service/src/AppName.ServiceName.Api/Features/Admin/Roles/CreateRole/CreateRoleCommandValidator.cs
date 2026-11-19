// @file-module:permissions
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using FluentValidation;

namespace AppName.ServiceName.Api.Features.Admin.Roles.CreateRole;

internal sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).ValidRoleName();

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
