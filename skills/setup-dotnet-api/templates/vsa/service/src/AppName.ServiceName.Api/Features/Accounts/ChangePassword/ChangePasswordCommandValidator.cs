// @file-module:auth
using AppName.ServiceName.Api.Features.Accounts.Shared;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AppName.ServiceName.Api.Features.Accounts.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator(IOptions<PasswordSettings> passwordSettings)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
                .WithErrorCode("CurrentPasswordRequired")
                .WithMessage("Current password is required");

        // Stop at the first failure, so a missing value never reaches the Must checks.
        RuleFor(x => x.NewPassword).Cascade(CascadeMode.Stop).ValidPassword(passwordSettings.Value, x => x.Username, x => x.EmailAddress);
    }
}
