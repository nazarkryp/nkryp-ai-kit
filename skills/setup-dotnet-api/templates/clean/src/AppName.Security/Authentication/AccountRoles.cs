// @file-module:permissions
namespace AppName.Security.Authentication;

/// <summary>
/// Roles that an account access token can carry.
/// </summary>
public static class AccountRoles
{
    /// <summary>The role claim of an admin token. An admin passes every permission check.</summary>
    public const string Admin = "Admin";
}
