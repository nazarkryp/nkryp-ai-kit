// @file-module:auth
namespace AppName.Api.Infrastructure.Security.Authentication;

/// <summary>
/// Names in the access token of an account.
/// </summary>
public static class AccountTokenDefaults
{
    /// <summary>
    /// One claim for each permission of an account that is not an admin. An admin token carries the
    /// Admin role instead.
    /// </summary>
    public const string PermissionClaimType = "perm";

    /// <summary>
    /// The JWT <c>typ</c> header of an access token (RFC 9068). The server accepts no other type, so a
    /// JWT of another kind cannot pass as an access token.
    /// </summary>
    public const string AccessTokenType = "at+jwt";
}
