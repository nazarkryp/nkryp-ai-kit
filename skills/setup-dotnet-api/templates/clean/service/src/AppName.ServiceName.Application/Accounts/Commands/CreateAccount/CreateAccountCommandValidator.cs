// @file-module:auth
using FluentValidation;
using AppName.ServiceName.Application.Accounts.Configurations;
using Microsoft.Extensions.Options;

namespace AppName.ServiceName.Application.Accounts.Commands.CreateAccount;

internal sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator(
        IOptions<AccountSettings> accountSettings,
        IOptions<PasswordSettings> passwordSettings)
    {
        var account = accountSettings.Value;
        var password = passwordSettings.Value;

        RuleFor(x => x.Username).Cascade(CascadeMode.Stop).ValidUsername(account);

        RuleFor(x => x.EmailAddress).Cascade(CascadeMode.Stop).ValidEmailAddress(account);

        RuleFor(x => x.Password).Cascade(CascadeMode.Stop).ValidPassword(password, x => x.Username, x => x.EmailAddress);
    }
}
