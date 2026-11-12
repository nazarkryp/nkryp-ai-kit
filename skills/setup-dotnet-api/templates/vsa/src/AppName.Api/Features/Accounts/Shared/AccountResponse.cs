// @file-module:auth
namespace AppName.Api.Features.Accounts.Shared;

public sealed class AccountResponse
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;
    // @module:permissions

    public bool IsAdmin { get; set; }

    /// <summary>The role of the account, or null when it has none.</summary>
    public Guid? RoleId { get; set; }

    public string? RoleName { get; set; }

    /// <summary>The permission codes of the account: its role and its direct grants. An admin has every code.</summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];
    // @end
}
