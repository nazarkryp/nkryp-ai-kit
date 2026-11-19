// @file-module:permissions
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;

namespace AppName.ServiceName.Api.Features.Admin.Accounts.CreateManagedAccount;

/// <param name="Account">The new account. It has no usable password yet.</param>
/// <param name="SetupCode">The code that the user enters to set a password. The server shows it only once.</param>
public sealed record CreatedManagedAccountResponse(ManagedAccountResponse Account, AccountSetupCodeResponse SetupCode);
