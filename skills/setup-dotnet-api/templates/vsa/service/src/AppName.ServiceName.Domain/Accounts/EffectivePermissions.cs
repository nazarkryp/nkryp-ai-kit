// @file-module:permissions
namespace AppName.ServiceName.Domain.Accounts;

/// <summary>What an account may do: the permissions of its role plus the permissions granted to it directly.</summary>
public sealed class EffectivePermissions
{
    private EffectivePermissions(bool isAdmin, IReadOnlyList<string> codes)
    {
        IsAdmin = isAdmin;
        Codes = codes;
    }

    public bool IsAdmin { get; }

    /// <summary>The permission codes in catalog order. An admin has every code.</summary>
    public IReadOnlyList<string> Codes { get; }

    /// <param name="role">The role of the account, or null when it has none.</param>
    /// <param name="grants">The permissions granted to the account directly.</param>
    public static EffectivePermissions For(Role? role, IEnumerable<string> grants)
    {
        if (role is { IsAdmin: true })
        {
            return new EffectivePermissions(true, Permissions.All.Select(x => x.Code).ToList());
        }

        var owned = new HashSet<string>(grants, StringComparer.Ordinal);

        if (role is not null)
        {
            owned.UnionWith(role.Permissions);
        }

        // A stored code that a later version renamed or removed, or an admin-only code, gives nothing.
        var codes = Permissions.All
            .Where(x => x.IsGrantable && owned.Contains(x.Code))
            .Select(x => x.Code)
            .ToList();

        return new EffectivePermissions(false, codes);
    }

    public bool Has(string code) => IsAdmin || Codes.Contains(code, StringComparer.Ordinal);
}
