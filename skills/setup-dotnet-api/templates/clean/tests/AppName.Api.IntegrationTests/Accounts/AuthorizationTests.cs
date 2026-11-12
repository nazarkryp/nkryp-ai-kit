// @file-module:auth
using System.Net;

namespace AppName.Api.IntegrationTests.Accounts;

/// <summary>
/// Which routes need a token and which do not.
/// </summary>
public sealed class AuthorizationTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task A_caller_without_a_token_gets_401_on_every_route_that_needs_a_sign_in()
    {
        var routes = TestRoute.SignedInRoutes(Factory.Services)
            // @module:permissions
            .Concat(TestRoute.PermissionRoutes(Factory.Services))
            // @end
            .ToList();
        var problems = new List<string>();

        routes.Should().NotBeEmpty();

        foreach (var route in routes)
        {
            using var response = await AnonymousClient.SendAsync(route.Request(), Ct);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                problems.Add($"{route}: {(int)response.StatusCode}");
            }
        }

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task Protected_route_accepts_a_signed_in_caller()
    {
        var response = await Client.GetAsync("/v1/accounts/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    public async Task Public_route_stays_open_under_the_fallback_policy(string route)
    {
        var response = await AnonymousClient.GetAsync(route, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_route_without_its_own_rule_needs_a_signed_in_caller()
    {
        // No endpoint matches, so only the fallback policy decides.
        var response = await AnonymousClient.GetAsync($"/v1/{Guid.NewGuid():N}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_broken_token_is_rejected()
    {
        using var client = CreateCookieFreeClient();

        client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-token");

        var response = await client.GetAsync("/v1/accounts/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_with_a_changed_payload_is_rejected()
    {
        var parts = Account.AccessToken.Split('.');
        var payload = parts[1].ToCharArray();

        // One changed character in the payload breaks the signature.
        payload[^2] = payload[^2] == 'A' ? 'B' : 'A';

        using var client = CreateClientFor(Account with { AccessToken = $"{parts[0]}.{new string(payload)}.{parts[2]}" });

        var response = await client.GetAsync("/v1/accounts/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_access_token_in_the_query_string_is_not_accepted()
    {
        var token = Uri.EscapeDataString(Account.AccessToken);

        var response = await AnonymousClient.GetAsync($"/v1/accounts/me?access_token={token}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
