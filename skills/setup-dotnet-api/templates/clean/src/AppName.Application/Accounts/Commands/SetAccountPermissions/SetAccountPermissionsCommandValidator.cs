// @file-module:permissions
using FluentValidation;

namespace AppName.Application.Accounts.Commands.SetAccountPermissions;

internal sealed class SetAccountPermissionsCommandValidator : AbstractValidator<SetAccountPermissionsCommand>
{
    public SetAccountPermissionsCommandValidator()
    {
        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
