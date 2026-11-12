// @file-module:auth
using AppName.Api.Infrastructure.Authorization;
// @module:permissions
using AppName.Api.Infrastructure.Security.Authorization;
// @end
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Infrastructure.Extensions;

/// <summary>
/// The authorization rule of a route. Every route has exactly one: a permission, any signed-in user, or
/// anonymous (<c>AllowAnonymous</c>). Put the rule on the route, not on its group: a group rule and a
/// route rule both apply.
/// </summary>
public static class AuthorizationEndpointExtensions
{
    // @module:permissions
    private static readonly ProducesResponseTypeMetadata Forbidden =
        new(StatusCodes.Status403Forbidden, typeof(ProblemDetails), ["application/problem+json"]);

    // @end
    private static readonly ProducesResponseTypeMetadata Unauthorized =
        new(StatusCodes.Status401Unauthorized, typeof(ProblemDetails), ["application/problem+json"]);
    // @module:permissions

    /// <summary>The route needs a signed-in account with this permission.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder
            .RequireAuthorization(new PermissionAuthorizeAttribute(permission))
            .WithMetadata(Unauthorized, Forbidden);

    /// <summary>The route needs a signed-in account with at least one of these permissions.</summary>
    public static TBuilder RequireAnyPermission<TBuilder>(this TBuilder builder, string first, params string[] others)
        where TBuilder : IEndpointConventionBuilder =>
        builder
            .RequireAuthorization(new PermissionAuthorizeAttribute([first, .. others]))
            .WithMetadata(Unauthorized, Forbidden);
    // @end

    /// <summary>Any signed-in account may call the route.</summary>
    public static TBuilder AllowAnySignedInUser<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder
            .RequireAuthorization()
            .WithMetadata(AnySignedInUserMetadata.Instance, Unauthorized);
}
