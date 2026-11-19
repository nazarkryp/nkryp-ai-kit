using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
// @module:jwt
using AppName.ServiceName.Api.Infrastructure.Authorization;
// @end
// @module:permissions
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Security.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
// @end

namespace AppName.ServiceName.Api.IntegrationTests.Security;

/// <summary>
/// Every route must have exactly one authorization rule, and it must be the rule of the approved route
/// map. A new route, a removed route or a changed rule fails here until the map below changes on
/// purpose. Without this test a route without a rule would get only the fallback policy, which lets
/// every signed-in account in.
/// <para>
/// A project without accounts has no authorization, but the test still runs: the map is the reviewed
/// list of public routes, and each route says <c>AllowAnonymous</c> on purpose.
/// </para>
/// </summary>
public sealed class EndpointAuthorizationCoverageTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly Rule Anonymous = new("anonymous", "");
    // @module:jwt
    private static readonly Rule SignedIn = new("any signed-in user", "");
    // @end
    private static readonly Rule NoRule = new("no rule", "");
    // @module:permissions

    private static Rule Permission(params string[] anyOf) =>
        new("permission", string.Join(", ", anyOf.Order(StringComparer.Ordinal)));
    // @end
    // @module:jwt

    private const string V = "v{v:apiVersion}";
    // @end

    /// <summary>The approved route map. An anonymous route checks its own secret or serves public data.</summary>
    private static readonly Dictionary<string, Rule> RouteMap = new(StringComparer.Ordinal)
    {
        // @module:auth
        // Accounts and sign-in.
        [$"POST {V}/accounts/"] = Anonymous,
        [$"POST {V}/accounts/signin"] = Anonymous,
        [$"POST {V}/accounts/token/refresh"] = Anonymous,
        [$"GET {V}/accounts/me"] = SignedIn,
        [$"PUT {V}/accounts/me/password"] = SignedIn,
        [$"POST {V}/accounts/signout"] = SignedIn,
        // @end
        // @module:trust
        // The example route of a service that trusts the tokens of the auth service.
        [$"GET {V}/me/"] = SignedIn,
        // @end
        // @module:permissions
        [$"POST {V}/accounts/setup"] = Anonymous,

        // Account management (admin only).
        [$"GET {V}/admin/accounts/"] = Permission(Permissions.AccountsManage),
        [$"GET {V}/admin/accounts/{{accountId:guid}}"] = Permission(Permissions.AccountsManage),
        [$"POST {V}/admin/accounts/"] = Permission(Permissions.AccountsManage),
        [$"PUT {V}/admin/accounts/{{accountId:guid}}/role"] = Permission(Permissions.AccountsManage),
        [$"PUT {V}/admin/accounts/{{accountId:guid}}/permissions"] = Permission(Permissions.AccountsManage),
        [$"POST {V}/admin/accounts/{{accountId:guid}}/sessions/revoke"] = Permission(Permissions.AccountsManage),
        [$"POST {V}/admin/accounts/{{accountId:guid}}/setup-code"] = Permission(Permissions.AccountsManage),
        [$"GET {V}/admin/roles/"] = Permission(Permissions.AccountsManage),
        [$"GET {V}/admin/roles/{{roleId:guid}}"] = Permission(Permissions.AccountsManage),
        [$"POST {V}/admin/roles/"] = Permission(Permissions.AccountsManage),
        [$"PUT {V}/admin/roles/{{roleId:guid}}"] = Permission(Permissions.AccountsManage),
        [$"DELETE {V}/admin/roles/{{roleId:guid}}"] = Permission(Permissions.AccountsManage),
        [$"GET {V}/admin/permissions/"] = Permission(Permissions.AccountsManage),
        // @end

        // Framework routes: the health check and the API documentation, in every environment.
        ["ANY /health"] = Anonymous,
        ["GET /openapi/{documentName}.json"] = Anonymous,
        ["GET /docs/{documentName?}"] = Anonymous,
        ["GET /docs/scalar.js"] = Anonymous,
        ["GET /docs/scalar.aspnetcore.js"] = Anonymous,
        ["GET /docs/favicon.svg"] = Anonymous,
    };

    [Fact]
    public void Every_route_has_the_rule_of_the_approved_route_map()
    {
        var actual = RouteEndpoints().ToDictionary(Name, ActualRule, StringComparer.Ordinal);
        var problems = new List<string>();

        problems.AddRange(RouteMap.Keys.Except(actual.Keys).Select(name => $"{name}: is in the route map but the host has no such route"));
        problems.AddRange(actual.Keys.Except(RouteMap.Keys).Select(name => $"{name}: is not in the route map (rule: {actual[name]})"));
        problems.AddRange(actual
            .Where(route => RouteMap.TryGetValue(route.Key, out var expected) && expected != route.Value)
            .Select(route => $"{route.Key}: has {route.Value}, the route map says {RouteMap[route.Key]}"));

        problems.Should().BeEmpty("the route map is the approved design:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Every_route_has_exactly_one_authorization_rule()
    {
        var problems = new List<string>();

        foreach (var endpoint in RouteEndpoints())
        {
            var metadata = endpoint.Metadata;
            var name = Name(endpoint);
            var rules = new List<string>();
            // @module:permissions

            var permissionRules = metadata.GetOrderedMetadata<PermissionAuthorizeAttribute>().Count;

            if (permissionRules > 0) rules.Add("permission");
            if (permissionRules > 1) problems.Add($"{name}: has {permissionRules} permission rules. Use one rule with the full any-of list");
            // @end
            // @module:jwt
            if (metadata.GetMetadata<AnySignedInUserMetadata>() is not null) rules.Add("any signed-in user");
            // @end
            if (metadata.GetMetadata<IAllowAnonymous>() is not null) rules.Add("anonymous");

            if (rules.Count != 1)
            {
                problems.Add($"{name}: needs exactly one rule, has [{string.Join(", ", rules)}]");
            }

            if (metadata.GetOrderedMetadata<IAuthorizeData>().Any(x => !string.IsNullOrEmpty(x.Roles)))
            {
                problems.Add($"{name}: checks a role. Check a permission instead");
            }

            if (metadata.GetMetadata<AuthorizationPolicy>() is not null)
            {
                problems.Add($"{name}: has an inline policy. Use a rule from AuthorizationEndpointExtensions");
            }
        }

        problems.Should().BeEmpty("every route needs one rule:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, problems));
    }
    // @module:permissions

    [Fact]
    public async Task A_permission_route_needs_a_signed_in_user_and_one_permission_requirement()
    {
        var policyProvider = Factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        var permissionRoutes = RouteEndpoints()
            .Where(x => x.Metadata.GetMetadata<PermissionAuthorizeAttribute>() is not null)
            .ToList();

        permissionRoutes.Should().NotBeEmpty();

        foreach (var endpoint in permissionRoutes)
        {
            // The same steps as the authorization middleware.
            var metadata = endpoint.Metadata;
            var policy = await AuthorizationPolicy.CombineAsync(
                policyProvider,
                metadata.GetOrderedMetadata<IAuthorizeData>(),
                metadata.GetOrderedMetadata<AuthorizationPolicy>());

            var requirements = (policy?.Requirements ?? [])
                .Concat(metadata.GetOrderedMetadata<IAuthorizationRequirementData>().SelectMany(x => x.GetRequirements()))
                .ToList();

            requirements.Should().ContainItemsAssignableTo<DenyAnonymousAuthorizationRequirement>(Name(endpoint));
            requirements.OfType<PermissionRequirement>().Should().ContainSingle(Name(endpoint));
        }
    }
    // @end

    private List<RouteEndpoint> RouteEndpoints() =>
        Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

    private static string Name(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"];

        return $"{string.Join(",", methods)} {endpoint.RoutePattern.RawText}";
    }

    private static Rule ActualRule(RouteEndpoint endpoint)
    {
        var metadata = endpoint.Metadata;
        // @module:permissions

        if (metadata.GetMetadata<PermissionAuthorizeAttribute>() is { } permission)
        {
            return Permission([.. permission.Requirement.AnyOf]);
        }
        // @end
        // @module:jwt

        if (metadata.GetMetadata<AnySignedInUserMetadata>() is not null) return SignedIn;
        // @end

        return metadata.GetMetadata<IAllowAnonymous>() is not null ? Anonymous : NoRule;
    }

    /// <param name="AnyOf">The permission codes in ordinal order, joined, so two rules compare by value.</param>
    private sealed record Rule(string Kind, string AnyOf)
    {
        public override string ToString() => AnyOf.Length == 0 ? Kind : $"{Kind}({AnyOf})";
    }
}
