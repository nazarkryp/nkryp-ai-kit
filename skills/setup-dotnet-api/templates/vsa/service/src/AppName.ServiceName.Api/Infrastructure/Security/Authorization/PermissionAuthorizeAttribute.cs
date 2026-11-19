// @file-module:permissions
using AppName.ServiceName.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;

namespace AppName.ServiceName.Api.Infrastructure.Security.Authorization;

/// <summary>
/// Endpoint metadata: the route needs a signed-in user with at least one of the permissions.
/// <para>
/// As <see cref="AuthorizeAttribute"/> without a policy, it adds the default policy, which needs a
/// signed-in user, so a request without a token gets 401, not 403. As
/// <see cref="IAuthorizationRequirementData"/>, it adds the <see cref="PermissionRequirement"/>. The
/// authorization middleware combines both with the other metadata of the endpoint.
/// </para>
/// </summary>
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public PermissionAuthorizeAttribute(params string[] anyOf)
    {
        if (anyOf.Length == 0)
        {
            throw new ArgumentException("A route needs at least one permission.", nameof(anyOf));
        }

        foreach (var code in anyOf)
        {
            if (!Permissions.IsKnown(code))
            {
                throw new ArgumentException($"'{code}' is not a permission in AppName.ServiceName.Domain.Accounts.Permissions.", nameof(anyOf));
            }
        }

        if (anyOf.Distinct(StringComparer.Ordinal).Count() != anyOf.Length)
        {
            throw new ArgumentException("A route lists a permission twice.", nameof(anyOf));
        }

        Requirement = new PermissionRequirement(anyOf);
    }

    public PermissionRequirement Requirement { get; }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return Requirement;
    }
}
