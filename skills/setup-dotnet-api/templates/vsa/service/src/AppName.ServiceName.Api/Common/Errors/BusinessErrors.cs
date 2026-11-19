using AppName.Api.Infrastructure.Common.Errors;

namespace AppName.ServiceName.Api.Common.Errors;

/// <summary>
/// Every error that a handler returns. Never create an error inline (<c>new ConflictError(...)</c>) in a
/// handler. Add it here and reference it, so each error name exists once.
/// </summary>
public static class BusinessErrors
{
    public static readonly ForbiddenError NotResourceOwner = new("NotResourceOwner", "Not resource owner");
    // @module:auth

    public static readonly NotFoundError UserNotFound = new("UserNotFound", "User not found");
    // @end
    // @module:permissions

    /// <summary>The account does not have any of the permissions that the action needs.</summary>
    public static ForbiddenError InsufficientPermissions(InsufficientPermissionsDetails details) => new(
        "InsufficientPermissions",
        $"This action needs one of these permissions: {string.Join(", ", details.RequiredAnyOf)}",
        details);
    // @end
    // @module:auth

    public static class Accounts
    {
        public static readonly ForbiddenError RegistrationDisabled = new("RegistrationDisabled", "User registration is currently disabled");

        public static readonly ValidationError UsernameConflict = new("UsernameConflict", "Username is already taken");

        public static readonly ValidationError InvalidCredentials = new("InvalidCredentials", "Invalid username or password");

        public static readonly ValidationError InvalidRefreshToken = new("InvalidRefreshToken", "Refresh token is invalid");

        public static readonly ValidationError RefreshTokenExpired = new("RefreshTokenExpired", "Refresh token has expired");

        public static readonly ValidationError CurrentPasswordInvalid = new(
            "CurrentPasswordInvalid",
            "The current password is not correct");
        // @module:permissions

        public static readonly ForbiddenError CannotChangeOwnRole = new(
            "CannotChangeOwnRole",
            "You cannot change your own role");

        public static readonly ConflictError LastAdmin = new(
            "LastAdmin",
            "The last admin account must keep the Admin role");

        public static readonly ValidationError InvalidSetupCode = new(
            "InvalidSetupCode",
            "The setup code is not valid or has expired");
        // @end
    }
    // @end
    // @module:permissions

    public static class Roles
    {
        public static NotFoundError NotFound(Guid roleId) => new(
            "RoleNotFound",
            $"Role '{roleId}' not found");

        public static readonly ConflictError NameConflict = new(
            "RoleNameConflict",
            "Another role has this name");

        public static readonly ConflictError InUse = new(
            "RoleInUse",
            "An account has this role. Give those accounts another role first");

        public static readonly ForbiddenError AdminRoleLocked = new(
            "AdminRoleLocked",
            "Nobody can change or delete the Admin role");
    }
    // @end
}
