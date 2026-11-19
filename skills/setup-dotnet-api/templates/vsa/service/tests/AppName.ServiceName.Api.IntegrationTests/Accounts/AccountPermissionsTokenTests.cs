// @file-module:permissions
using System.Net;
using System.Text;
using System.Text.Json;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Features.Accounts.SignIn;
using AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountPermissions;
using AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountRole;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>
/// Sign-in and refresh write the permissions of the role and the direct grants into the access token
/// and into the token response. The admin sets the role and the grants through the API.
/// </summary>
public sealed class AccountPermissionsTokenTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string RoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private const string PermissionClaim = "perm";

    [Fact]
    public async Task The_admin_gets_every_permission_and_a_token_with_the_role_but_no_permission_claims()
    {
        var token = await SignInForTokenAsync(Factory.AdminAccount!);

        token.IsAdmin.Should().BeTrue();
        token.Permissions.Should().Equal(Permissions.All.Select(x => x.Code));
        Header(token.AccessToken).GetProperty("typ").GetString().Should().Be("at+jwt");
        Claims(token.AccessToken, RoleClaim).Should().Equal("Admin");
        Claims(token.AccessToken, PermissionClaim).Should().BeEmpty();
    }

    [Fact]
    public async Task A_new_account_has_no_role_and_no_permission()
    {
        var account = await CreateBaseAccountAsync();

        var token = await SignInForTokenAsync(account);

        token.IsAdmin.Should().BeFalse();
        token.Permissions.Should().BeEmpty();
        Header(token.AccessToken).GetProperty("typ").GetString().Should().Be("at+jwt");
        Claims(token.AccessToken, RoleClaim).Should().BeEmpty();
        Claims(token.AccessToken, PermissionClaim).Should().BeEmpty();

        var me = await GetMeAsync(token.AccessToken);

        me.IsAdmin.Should().BeFalse();
        me.RoleId.Should().BeNull();
        me.RoleName.Should().BeNull();
        me.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task The_role_and_the_direct_grants_reach_the_token_and_me_after_a_refresh()
    {
        using var admin = CreateAdminClient();
        var account = await CreateBaseAccountAsync();
        var accountId = await GetAccountIdAsync(account);
        var roleName = $"Editors {Guid.NewGuid():N}";

        // The role has the second half of the grantable permissions, the grants have the first half.
        var half = GrantablePermissions.Count / 2;
        var created = await admin.PostJsonAsync("/v1/admin/roles", new SaveRoleRequest(roleName, [.. GrantablePermissions.Skip(half)]));
        await created.EnsureSuccessAsync();
        var role = await created.ReadRequiredAsync<RoleResponse>();

        await (await admin.PutJsonAsync($"/v1/admin/accounts/{accountId}/role", new SetAccountRoleRequest(role.Id))).EnsureSuccessAsync();
        await (await admin.PutJsonAsync(
            $"/v1/admin/accounts/{accountId}/permissions",
            new SetAccountPermissionsRequest([.. GrantablePermissions.Take(half)]))).EnsureSuccessAsync();

        var response = await RefreshAsync(account.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.ReadRequiredAsync<TokenResponse>();

        token.IsAdmin.Should().BeFalse();
        token.Permissions.Should().Equal(GrantablePermissions);
        Claims(token.AccessToken, PermissionClaim).Should().Equal(GrantablePermissions);

        var me = await GetMeAsync(token.AccessToken);

        me.RoleId.Should().Be(role.Id);
        me.RoleName.Should().Be(roleName);
        me.Permissions.Should().Equal(GrantablePermissions);
    }

    private async Task<TokenResponse> SignInForTokenAsync(TestAccount account)
    {
        var response = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = account.Username, Password = account.Password });

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<TokenResponse>();
    }

    private async Task<AccountResponse> GetMeAsync(string accessToken)
    {
        using var client = CreateClientFor(Account with { AccessToken = accessToken });

        var response = await client.GetAsync("/v1/accounts/me", Ct);

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<AccountResponse>();
    }

    private static JsonElement Header(string jwt) => Decode(jwt.Split('.')[0]);

    /// <summary>The values of one claim. The token holds one value as a string and more values as an array.</summary>
    private static List<string> Claims(string jwt, string type)
    {
        var payload = Decode(jwt.Split('.')[1]);

        if (!payload.TryGetProperty(type, out var claim))
        {
            return [];
        }

        return claim.ValueKind == JsonValueKind.Array
            ? claim.EnumerateArray().Select(x => x.GetString()!).ToList()
            : [claim.GetString()!];
    }

    private static JsonElement Decode(string part)
    {
        var base64 = part.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64))).RootElement.Clone();
    }
}
