// @file-module:permissions
using FluentValidation;

namespace AppName.Application.Accounts.Commands.SetAccountRole;

internal sealed class SetAccountRoleCommandValidator : AbstractValidator<SetAccountRoleCommand>
{
    public SetAccountRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .Must(roleId => roleId != Guid.Empty)
                .WithErrorCode("RoleIdInvalid")
                .WithMessage("Role id is not valid")
            .When(x => x.RoleId.HasValue);
    }
}
