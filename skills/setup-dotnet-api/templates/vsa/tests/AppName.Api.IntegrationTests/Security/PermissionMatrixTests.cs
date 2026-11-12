// @file-module:permissions
using System.Net;
using System.Text.Json;
using AppName.Api.Features.Accounts.Shared;
using AppName.Api.Features.Admin.Accounts.SetAccountRole;
using AppName.Api.Features.Admin.Roles.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.IntegrationTests.Security;

/// <summary>
/// Each permission route against accounts with and without the permission. The routes come from the
/// running host, so a new route is in the matrix without a change here.
/// </summary>
public sealed class PermissionMatrixTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task An_account_without_the_permission_gets_403_with_the_required_permissions()
    {
        using var client = CreateClientFor(await CreateBaseAccountAsync());
        var routes = PermissionRoutes().ToList();
        var problems = new List<string>();

        routes.Should().NotBeEmpty();

        foreach (var route in routes)
        {
            using var response = await client.SendAsync(route.Request(), Ct);

            if (response.StatusCode != HttpStatusCode.Forbidden)
            {
                problems.Add($"{route}: {(int)response.StatusCode}");
                continue;
            }

            var problem = await response.ReadRequiredAsync<ProblemDetails>();
            var required = ((JsonElement)problem.Extensions["details"]!)
                .GetProperty("requiredAnyOf")
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToList();

            if (problem.Type != "InsufficientPermissions" || !required.SequenceEqual(route.AnyOf))
            {
                problems.Add($"{route}: {problem.Type} [{string.Join(", ", required)}]");
            }
        }

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task One_grantable_permission_opens_its_routes_and_no_other()
    {
        var problems = new List<string>();
        var readOnlyRoutes = ReadOnlyPermissionRoutes().ToList();

        // Each grantable permission must open at least one route here, or this test proves nothing for it.
        GrantablePermissions.Should().OnlyContain(permission => readOnlyRoutes.Any(route => route.AnyOf.Contains(permission)));

        foreach (var permission in GrantablePermissions)
        {
            using var client = CreateClientFor(await CreateAccountWithPermissionsAsync(permission));

            foreach (var route in readOnlyRoutes)
            {
                using var response = await client.SendAsync(route.Request(), Ct);

                // A random id makes 404 a correct answer, but a server error never is.
                if ((int)response.StatusCode >= 500)
                {
                    problems.Add($"{permission} on {route}: {(int)response.StatusCode}");
                    continue;
                }

                var allowed = response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

                if (allowed != route.AnyOf.Contains(permission))
                {
                    problems.Add($"{permission} on {route}: {(int)response.StatusCode}");
                }
            }
        }

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task The_admin_passes_every_permission_check()
    {
        using var client = CreateAdminClient();
        var routes = ReadOnlyPermissionRoutes().ToList();
        var problems = new List<string>();

        routes.Should().NotBeEmpty();

        foreach (var route in routes)
        {
            using var response = await client.SendAsync(route.Request(), Ct);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                || (int)response.StatusCode >= 500)
            {
                problems.Add($"{route}: {(int)response.StatusCode}");
            }
        }

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task A_role_gives_its_permissions_and_never_an_admin_only_one()
    {
        using var admin = CreateAdminClient();
        var account = await CreateBaseAccountAsync();

        var created = await admin.PostJsonAsync(
            "/v1/admin/roles",
            new SaveRoleRequest($"Members {Guid.NewGuid():N}", [.. GrantablePermissions]));
        await created.EnsureSuccessAsync();
        var role = await created.ReadRequiredAsync<RoleResponse>();

        var accountId = await GetAccountIdAsync(account);
        var assigned = await admin.PutJsonAsync($"/v1/admin/accounts/{accountId}/role", new SetAccountRoleRequest(role.Id));
        await assigned.EnsureSuccessAsync();

        var member = await SignInAsync(account.Username, account.EmailAddress, account.Password);
        using var client = CreateClientFor(member);

        var me = await (await client.GetAsync("/v1/accounts/me", Ct)).ReadRequiredAsync<AccountResponse>();
        me.Permissions.Should().Equal(GrantablePermissions);

        (await client.GetAsync("/v1/admin/accounts", Ct)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private IEnumerable<TestRoute> PermissionRoutes() => TestRoute.PermissionRoutes(Factory.Services);

    /// <summary>
    /// The GET routes. A test calls them with the permission, so they must not change data.
    /// </summary>
    private IEnumerable<TestRoute> ReadOnlyPermissionRoutes() =>
        PermissionRoutes().Where(x => x.Method == HttpMethod.Get);
}
