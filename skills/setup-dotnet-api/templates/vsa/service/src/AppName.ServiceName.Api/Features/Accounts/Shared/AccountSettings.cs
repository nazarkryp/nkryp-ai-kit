// @file-module:auth
namespace AppName.ServiceName.Api.Features.Accounts.Shared;

public sealed class AccountSettings
{
    public const string SectionName = "AccountSettings";

    public int MinUsernameLength { get; init; } = 3;
    public int MaxUsernameLength { get; init; } = 20;
    public string UsernameRegexPattern { get; init; } = "^[a-zA-Z0-9._]+$";
    public string EmailAddressPattern { get; init; } = "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$";
    public bool AllowUserRegistration { get; init; } = false;
    public IReadOnlyList<string> BlockedWords { get; init; } = [];
    public IReadOnlyList<string> SystemUsernames { get; init; } = [];
}
