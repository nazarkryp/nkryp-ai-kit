// @file-module:permissions
using AppName.Api.Authentication;
using AppName.ServiceName.Security.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace AppName.ServiceName.Security.Authorization;

/// <summary>
/// Passes a <see cref="PermissionRequirement"/> when the access token has one of its permissions, or
/// when the user is an admin.
/// <para>
/// The admin check is here and not in a handler for every requirement: such a handler would also pass
/// the requirements of other policies, for example the Worker policy.
/// </para>
/// <para>
/// The handler never calls <see cref="AuthorizationHandlerContext.Fail()"/>. A missing permission
/// leaves the requirement open, so another handler for it (for example a later library access check)
/// still runs.
/// </para>
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;

        if (user.Identity is { IsAuthenticated: true }
            && (user.IsInRole(AccountRoles.Admin)
                || requirement.AnyOf.Any(code => user.HasClaim(AccountTokenDefaults.PermissionClaimType, code))))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
