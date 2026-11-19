// @file-module:auth
namespace AppName.ServiceName.Api.Features.Accounts.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
