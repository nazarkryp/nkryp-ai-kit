// @file-module:permissions
using System.Net;
using System.Text;
using System.Text.Json;
using AppName.ServiceName.Api.Features.Accounts.Shared;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>The first account on a database is the admin. Later accounts are not.</summary>
public sealed class AdminRoleTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string RoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    [Fact]
    public async Task The_first_account_is_an_admin()
    {
        using var client = CreateAdminClient();

        var response = await client.GetAsync("/v1/accounts/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadRequiredAsync<AccountResponse>()).IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task A_later_account_is_not_an_admin()
    {
        var account = await CreateAccountAsync();

        using var client = CreateClientFor(account);
        var response = await client.GetAsync("/v1/accounts/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadRequiredAsync<AccountResponse>()).IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Only_the_admin_token_carries_the_role_claim()
    {
        var other = await CreateAccountAsync();

        Roles(Factory.AdminAccount!.AccessToken).Should().Contain("Admin");
        Roles(other.AccessToken).Should().NotContain("Admin");
    }

    private static List<string> Roles(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

        if (!document.RootElement.TryGetProperty(RoleClaim, out var role))
        {
            return [];
        }

        return role.ValueKind == JsonValueKind.Array
            ? role.EnumerateArray().Select(r => r.GetString()!).ToList()
            : [role.GetString()!];
    }
}
