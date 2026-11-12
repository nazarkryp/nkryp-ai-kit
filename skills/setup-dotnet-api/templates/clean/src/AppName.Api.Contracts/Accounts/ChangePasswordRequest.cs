// @file-module:auth
namespace AppName.Api.Contracts.Accounts;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
