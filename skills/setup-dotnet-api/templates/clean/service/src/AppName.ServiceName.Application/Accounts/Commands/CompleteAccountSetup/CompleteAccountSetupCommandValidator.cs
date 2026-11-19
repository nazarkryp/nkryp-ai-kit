// @file-module:permissions
using FluentValidation;

namespace AppName.ServiceName.Application.Accounts.Commands.CompleteAccountSetup;

internal sealed class CompleteAccountSetupCommandValidator : AbstractValidator<CompleteAccountSetupCommand>
{
    public CompleteAccountSetupCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
                .WithErrorCode("SetupCodeRequired")
                .WithMessage("Setup code is required");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithErrorCode("PasswordRequired")
                .WithMessage("Password is required");
    }
}
