// @file-module:permissions
namespace AppName.Domain.Accounts;

/// <summary>A named set of permissions. An account has one role or none.</summary>
public sealed class Role
{
    /// <summary>
    /// The Admin role. It has every permission, also the ones added later, so it stores none. Nobody can
    /// edit or delete it.
    /// </summary>
    public static readonly Guid AdminId = new("b2ee69ff-4dc4-439b-ba6b-ca58b12247c3");

    public const string AdminName = "Admin";

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>The stored permission codes. A code that is unknown or admin-only gives nothing.</summary>
    public required IReadOnlySet<string> Permissions { get; init; }

    public bool IsAdmin => Id == AdminId;
}
