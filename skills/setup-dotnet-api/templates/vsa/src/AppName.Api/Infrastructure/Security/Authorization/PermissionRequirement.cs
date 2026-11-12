// @file-module:permissions
using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;

namespace AppName.Api.Infrastructure.Security.Authorization;

/// <summary>
/// The user needs at least one of <see cref="AnyOf"/>. ASP.NET Core combines several requirements with
/// AND, so one requirement holds the whole list to express OR.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(IEnumerable<string> anyOf)
    {
        // An immutable copy: the middleware caches the policy of an endpoint and shares this instance
        // between all requests.
        AnyOf = [.. anyOf];
    }

    public ImmutableArray<string> AnyOf { get; }

    public override string ToString() => $"Permission: any of {string.Join(", ", AnyOf)}";
}
