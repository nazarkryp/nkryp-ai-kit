// @file-module:permissions
using System.Collections.Frozen;

namespace AppName.Domain.Accounts;

/// <param name="Code">The stable code. The database and the access tokens store it, so it never changes.</param>
/// <param name="Description">What the permission allows. The admin reads it before granting the permission.</param>
/// <param name="IsGrantable">
/// False for an admin-only permission. Only the Admin role has it. Nobody can grant it to an account or put it in a role.
/// </param>
public sealed record PermissionDefinition(string Code, string Description, bool IsGrantable);

/// <summary>
/// Every permission that a route can require. A code is permanent: to rename or remove one, add a data
/// step to the persistence initializer that changes the stored rows.
/// </summary>
public static class Permissions
{
    public const string AccountsManage = "accounts.manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(AccountsManage, "Manage accounts, roles and permissions. Admin only.", false),
    ];

    private static readonly FrozenDictionary<string, PermissionDefinition> ByCode =
        All.ToFrozenDictionary(x => x.Code, StringComparer.Ordinal);

    public static bool IsKnown(string code) => ByCode.ContainsKey(code);

    public static bool IsGrantable(string code) => ByCode.TryGetValue(code, out var definition) && definition.IsGrantable;
}
