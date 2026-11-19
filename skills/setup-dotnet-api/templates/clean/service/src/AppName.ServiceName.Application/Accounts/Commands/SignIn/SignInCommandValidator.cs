// @file-module:auth
using FluentValidation;

namespace AppName.ServiceName.Application.Accounts.Commands.SignIn;

internal sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithErrorCode("UsernameRequired")
            .WithMessage("Username is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode("PasswordRequired")
            .WithMessage("Password is required");
    }
}
