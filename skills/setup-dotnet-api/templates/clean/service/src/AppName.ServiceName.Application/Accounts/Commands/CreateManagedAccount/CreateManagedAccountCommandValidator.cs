// @file-module:permissions
using FluentValidation;
using AppName.ServiceName.Application.Accounts.Configurations;
using Microsoft.Extensions.Options;

namespace AppName.ServiceName.Application.Accounts.Commands.CreateManagedAccount;

internal sealed class CreateManagedAccountCommandValidator : AbstractValidator<CreateManagedAccountCommand>
{
    public CreateManagedAccountCommandValidator(IOptions<AccountSettings> accountSettings)
    {
        var account = accountSettings.Value;

        // Stop at the first failure, so a missing value never reaches the Must checks.
        RuleFor(x => x.Username).Cascade(CascadeMode.Stop).ValidUsername(account);

        RuleFor(x => x.EmailAddress).Cascade(CascadeMode.Stop).ValidEmailAddress(account);

        RuleFor(x => x.RoleId)
            .Must(roleId => roleId != Guid.Empty)
                .WithErrorCode("RoleIdInvalid")
                .WithMessage("Role id is not valid")
            .When(x => x.RoleId.HasValue);

        RuleFor(x => x.Permissions).GrantablePermissions();
    }
}
