// @file-module:auth
namespace AppName.ServiceName.Api.Features.Accounts.Shared;

/// <summary>
/// A new password together with the account it is for. A handler validates it when the request does
/// not carry the username, for example an account setup that has only the setup code.
/// </summary>
public sealed record AccountPassword(string Password, string Username, string EmailAddress);
