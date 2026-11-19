// @file-module:permissions
namespace AppName.ServiceName.Domain.Accounts;

/// <summary>A role together with the number of accounts that have it.</summary>
public sealed record RoleUsage(Role Role, int AccountCount);
