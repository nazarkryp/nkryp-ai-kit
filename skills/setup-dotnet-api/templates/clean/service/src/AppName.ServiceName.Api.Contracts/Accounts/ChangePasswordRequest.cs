// @file-module:auth
namespace AppName.ServiceName.Api.Contracts.Accounts;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
