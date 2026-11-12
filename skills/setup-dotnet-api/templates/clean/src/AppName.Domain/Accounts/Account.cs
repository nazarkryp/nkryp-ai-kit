// @file-module:auth
namespace AppName.Domain.Accounts;

public sealed class Account
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;
    // @module:permissions

    /// <summary>The role of the account, or null when it has none.</summary>
    public Guid? RoleId { get; set; }

    /// <summary>The permissions granted to this account directly, on top of its role.</summary>
    public IReadOnlyCollection<string> Grants { get; set; } = [];

    public bool IsAdmin => RoleId == Role.AdminId;
    // @end
}
