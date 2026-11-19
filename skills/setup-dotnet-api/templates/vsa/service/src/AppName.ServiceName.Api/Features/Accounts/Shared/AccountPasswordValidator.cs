// @file-module:auth
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AppName.ServiceName.Api.Features.Accounts.Shared;

internal sealed class AccountPasswordValidator : AbstractValidator<AccountPassword>
{
    public AccountPasswordValidator(IOptions<PasswordSettings> passwordSettings)
    {
        // Stop at the first failure, so a missing value never reaches the Must checks.
        RuleFor(x => x.Password).Cascade(CascadeMode.Stop).ValidPassword(passwordSettings.Value, x => x.Username, x => x.EmailAddress);
    }
}
