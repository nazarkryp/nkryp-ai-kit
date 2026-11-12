// @file-module:auth
namespace AppName.Application.Accounts.Configurations;

public sealed class PasswordSettings
{
    public const string SectionName = "PasswordSettings";

    public int MinPasswordLength { get; init; } = 8;
    public int MaxPasswordLength { get; init; } = 128;
    public bool RequireUppercase { get; init; } = true;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireSpecialCharacter { get; init; } = true;
    public string SpecialCharacters { get; init; } = "!@#$%^&*()-_=+[]{}|;:,.<>?";
    public bool DisallowUsernameInPassword { get; init; } = true;
    public bool DisallowEmailInPassword { get; init; } = true;
    public int MaxConsecutiveCharacters { get; init; } = 3;
    public bool AllowWhitespace { get; init; } = false;
    public IReadOnlyList<string> BlockedPasswords { get; init; } = [];
}
