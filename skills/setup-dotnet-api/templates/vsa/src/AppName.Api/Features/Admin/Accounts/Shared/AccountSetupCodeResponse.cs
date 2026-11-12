// @file-module:permissions
namespace AppName.Api.Features.Admin.Accounts.Shared;

/// <param name="Code">The one-time setup code. The server stores only its hash, so it shows the code only once.</param>
public sealed record AccountSetupCodeResponse(string Code, DateTime ExpiresAt);
