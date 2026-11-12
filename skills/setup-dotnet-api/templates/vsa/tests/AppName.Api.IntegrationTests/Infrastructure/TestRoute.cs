// @file-module:auth
using System.Text.RegularExpressions;
using AppName.Api.Infrastructure.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
// @module:permissions
using AppName.Api.Infrastructure.Security.Authorization;
// @end

namespace AppName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// A request to a route of the running host. The routes come from the host, so a new route is in the
/// authorization tests without a change there.
/// </summary>
/// <param name="AnyOf">The permissions that the route needs. Empty for a route without a permission.</param>
public sealed partial record TestRoute(HttpMethod Method, string Path, IReadOnlyList<string> AnyOf)
{
    /// <summary>The routes that any signed-in user may call.</summary>
    public static IEnumerable<TestRoute> SignedInRoutes(IServiceProvider services) =>
        RouteEndpoints(services)
            .Where(x => x.Metadata.GetMetadata<AnySignedInUserMetadata>() is not null)
            .SelectMany(x => For(x, []));
    // @module:permissions

    /// <summary>The routes that need a permission.</summary>
    public static IEnumerable<TestRoute> PermissionRoutes(IServiceProvider services) =>
        RouteEndpoints(services)
            .Select(x => (Endpoint: x, Rule: x.Metadata.GetMetadata<PermissionAuthorizeAttribute>()))
            .Where(x => x.Rule is not null)
            .SelectMany(x => For(x.Endpoint, x.Rule!.Requirement.AnyOf));
    // @end

    /// <summary>The body is empty: the authorization middleware decides before model binding.</summary>
    public HttpRequestMessage Request() => new(Method, Path);

    public override string ToString() => $"{Method} {Path}";

    private static List<RouteEndpoint> RouteEndpoints(IServiceProvider services) =>
        services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

    private static IEnumerable<TestRoute> For(RouteEndpoint endpoint, IEnumerable<string> anyOf)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
        var path = "/" + Parameter().Replace(endpoint.RoutePattern.RawText!.TrimStart('/'), ValueFor).TrimEnd('/');
        var codes = anyOf.ToList();

        return methods.Select(method => new TestRoute(new HttpMethod(method), path, codes));
    }

    /// <summary>A value that matches the parameter constraint.</summary>
    private static string ValueFor(Match parameter)
    {
        var text = parameter.Groups[1].Value;

        if (text.StartsWith("v:", StringComparison.Ordinal)) return "1";
        if (text.Contains(":guid", StringComparison.Ordinal)) return Guid.NewGuid().ToString();
        if (text.Contains(":int", StringComparison.Ordinal)) return "1";

        return "x";
    }

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex Parameter();
}
