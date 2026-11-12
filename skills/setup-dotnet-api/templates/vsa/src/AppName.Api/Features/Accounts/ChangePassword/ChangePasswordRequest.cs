// @file-module:auth
namespace AppName.Api.Features.Accounts.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
