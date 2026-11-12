// @file-module:auth
using FluentValidation;
using AppName.Application.Accounts.Configurations;

namespace AppName.Application.Accounts;

/// <summary>
/// The username, email address and password rules. Registration, an account that an admin creates,
/// the account setup and the password change all use them, so the error codes are the same everywhere.
/// </summary>
internal static class AccountRules
{
    public static IRuleBuilderOptions<T, string> ValidUsername<T>(this IRuleBuilder<T, string> rule, AccountSettings account) =>
        rule
            .NotEmpty()
                .WithErrorCode("NameRequired")
                .WithMessage("Name is required")
            .MinimumLength(account.MinUsernameLength)
                .WithErrorCode("UsernameTooShort")
                .WithMessage($"Username must be at least {account.MinUsernameLength} characters")
            .MaximumLength(account.MaxUsernameLength)
                .WithErrorCode("UsernameTooLong")
                .WithMessage($"Username must not exceed {account.MaxUsernameLength} characters")
            .Matches(account.UsernameRegexPattern)
                .WithErrorCode("UsernameInvalid")
                .WithMessage("Username contains invalid characters")
            .Must(name => !account.BlockedWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase)))
                .WithErrorCode("UsernameBlocked")
                .WithMessage("Username contains a blocked word")
            .Must(name => !account.SystemUsernames.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .WithErrorCode("UsernameReserved")
                .WithMessage("Username is reserved");

    public static IRuleBuilderOptions<T, string> ValidEmailAddress<T>(this IRuleBuilder<T, string> rule, AccountSettings account) =>
        rule
            .NotEmpty()
                .WithErrorCode("EmailRequired")
                .WithMessage("Email address is required")
            .Matches(account.EmailAddressPattern)
                .WithErrorCode("EmailInvalid")
                .WithMessage("Email address is not valid");

    /// <param name="username">The username that the password must not contain.</param>
    /// <param name="emailAddress">The email address that the password must not contain.</param>
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(
        this IRuleBuilder<T, string> rule,
        PasswordSettings password,
        Func<T, string?> username,
        Func<T, string?> emailAddress) =>
        rule
            .NotEmpty()
                .WithErrorCode("PasswordRequired")
                .WithMessage("Password is required")
            .MinimumLength(password.MinPasswordLength)
                .WithErrorCode("PasswordTooShort")
                .WithMessage($"Password must be at least {password.MinPasswordLength} characters")
            .MaximumLength(password.MaxPasswordLength)
                .WithErrorCode("PasswordTooLong")
                .WithMessage($"Password must not exceed {password.MaxPasswordLength} characters")
            .Must(p => !password.RequireUppercase || p.Any(char.IsUpper))
                .WithErrorCode("PasswordRequiresUppercase")
                .WithMessage("Password must contain at least one uppercase letter")
            .Must(p => !password.RequireLowercase || p.Any(char.IsLower))
                .WithErrorCode("PasswordRequiresLowercase")
                .WithMessage("Password must contain at least one lowercase letter")
            .Must(p => !password.RequireDigit || p.Any(char.IsDigit))
                .WithErrorCode("PasswordRequiresDigit")
                .WithMessage("Password must contain at least one digit")
            .Must(p => !password.RequireSpecialCharacter || p.Any(c => password.SpecialCharacters.Contains(c)))
                .WithErrorCode("PasswordRequiresSpecialCharacter")
                .WithMessage("Password must contain at least one special character")
            .Must(p => password.AllowWhitespace || !p.Any(char.IsWhiteSpace))
                .WithErrorCode("PasswordContainsWhitespace")
                .WithMessage("Password must not contain whitespace")
            .Must(p => HasNoConsecutiveRepeats(p, password.MaxConsecutiveCharacters))
                .WithErrorCode("PasswordConsecutiveCharacters")
                .WithMessage($"Password must not contain more than {password.MaxConsecutiveCharacters} consecutive identical characters")
            .Must(p => !password.BlockedPasswords.Any(b => b.Equals(p, StringComparison.OrdinalIgnoreCase)))
                .WithErrorCode("PasswordBlocked")
                .WithMessage("Password is too common")
            .Must((x, p) => !password.DisallowUsernameInPassword ||
                            string.IsNullOrEmpty(username(x)) ||
                            !p.Contains(username(x)!, StringComparison.OrdinalIgnoreCase))
                .WithErrorCode("PasswordContainsUsername")
                .WithMessage("Password must not contain the username")
            .Must((x, p) => !password.DisallowEmailInPassword ||
                            string.IsNullOrEmpty(emailAddress(x)) ||
                            !p.Contains(emailAddress(x)!, StringComparison.OrdinalIgnoreCase))
                .WithErrorCode("PasswordContainsEmail")
                .WithMessage("Password must not contain the email address");

    private static bool HasNoConsecutiveRepeats(string value, int max)
    {
        var count = 1;
        for (var i = 1; i < value.Length; i++)
        {
            count = value[i] == value[i - 1] ? count + 1 : 1;
            if (count > max)
                return false;
        }

        return true;
    }
}
