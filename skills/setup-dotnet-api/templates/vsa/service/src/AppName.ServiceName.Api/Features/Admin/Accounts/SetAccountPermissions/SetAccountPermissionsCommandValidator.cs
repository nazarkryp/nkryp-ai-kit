// @file-module:permissions
using AppName.ServiceName.Api.Features.Accounts.Shared;
using FluentValidation;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountPermissions;

internal sealed class SetAccountPermissionsCommandValidator : AbstractValidator<SetAccountPermissionsCommand>
{
    public SetAccountPermissionsCommandValidator()
    {
        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
