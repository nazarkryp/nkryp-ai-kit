// @file-module:auth
using FluentValidation;
using AppName.Application.Accounts.Configurations;
using Microsoft.Extensions.Options;

namespace AppName.Application.Accounts;

/// <summary>
/// A new password together with the account it is for. A handler validates it when the request does
/// not carry the username, for example an account setup that has only the setup code.
/// </summary>
public sealed record AccountPassword(string Password, string Username, string EmailAddress);

internal sealed class AccountPasswordValidator : AbstractValidator<AccountPassword>
{
    public AccountPasswordValidator(IOptions<PasswordSettings> passwordSettings)
    {
        // Stop at the first failure, so a missing value never reaches the Must checks.
        RuleFor(x => x.Password).Cascade(CascadeMode.Stop).ValidPassword(passwordSettings.Value, x => x.Username, x => x.EmailAddress);
    }
}
