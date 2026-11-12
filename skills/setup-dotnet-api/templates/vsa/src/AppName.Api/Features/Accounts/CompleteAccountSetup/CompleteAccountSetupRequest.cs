// @file-module:permissions
namespace AppName.Api.Features.Accounts.CompleteAccountSetup;

public sealed record CompleteAccountSetupRequest(string Code, string Password);
