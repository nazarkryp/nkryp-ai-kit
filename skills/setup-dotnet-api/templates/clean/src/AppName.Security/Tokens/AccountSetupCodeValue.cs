// @file-module:permissions
namespace AppName.Security.Tokens;

/// <param name="Value">The code that the user enters. It is shown once and never stored.</param>
/// <param name="Hash">The SHA-256 hash of <paramref name="Value"/>, which the database stores.</param>
public sealed record AccountSetupCodeValue(string Value, string Hash);
