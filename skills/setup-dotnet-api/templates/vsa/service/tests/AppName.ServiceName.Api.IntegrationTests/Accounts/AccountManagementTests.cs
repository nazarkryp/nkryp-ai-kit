// @file-module:permissions
using System.Net;
using System.Text;
using AppName.Api.Infrastructure.Common;
using AppName.ServiceName.Api.Features.Accounts.CompleteAccountSetup;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Features.Accounts.SignIn;
using AppName.ServiceName.Api.Features.Admin.Accounts.CreateManagedAccount;
using AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountPermissions;
using AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountRole;
using AppName.ServiceName.Api.Features.Admin.Accounts.Shared;
using AppName.ServiceName.Api.Features.Admin.Roles.Shared;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>
/// The admin creates accounts, sets their role and grants, ends their sessions and issues setup codes
/// through <c>/v1/admin/accounts</c>.
/// </summary>
public sealed class AccountManagementTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task An_account_with_every_grantable_permission_cannot_manage_accounts()
    {
        var response = await Client.GetAsync("/v1/admin/accounts", Ct);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "InsufficientPermissions");
    }

    [Fact]
    public async Task The_admin_creates_an_account_with_a_role_and_grants_and_gets_a_setup_code()
    {
        using var admin = CreateAdminClient();
        var role = await CreateRoleAsync(admin, [.. GrantablePermissions]);
        var username = NewUsername();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(
                username,
                $"{username}@example.test",
                role.Id,
                [.. GrantablePermissions.Reverse()]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.ReadRequiredAsync<CreatedManagedAccountResponse>();
        response.Headers.Location!.OriginalString.Should().Be($"/v1/admin/accounts/{created.Account.Id}");

        created.Account.Username.Should().Be(username);
        created.Account.IsAdmin.Should().BeFalse();
        created.Account.RoleId.Should().Be(role.Id);
        created.Account.RoleName.Should().Be(role.Name);
        created.Account.Grants.Should().Equal(GrantablePermissions);
        created.Account.Permissions.Should().Equal(GrantablePermissions);

        created.SetupCode.Code.Should().HaveLength(22);
        created.SetupCode.ExpiresAt.ToUniversalTime().Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));

        // Before the setup, a sign-in with the password that the setup tests use fails.
        var signIn = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = username, Password = Password });

        signIn.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_admin_lists_and_gets_a_new_account_and_an_unknown_id_is_not_found()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync(admin);

        var list = await admin.GetAsync("/v1/admin/accounts", Ct);

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = (await list.ReadRequiredAsync<CollectionResponse<ManagedAccountResponse>>()).Data
            .Should().ContainSingle(x => x.Id == created.Account.Id).Subject;
        listed.Username.Should().Be(created.Account.Username);
        listed.SetupCodeExpiresAt.Should().NotBeNull();

        var one = await admin.GetAsync($"/v1/admin/accounts/{created.Account.Id}", Ct);

        one.StatusCode.Should().Be(HttpStatusCode.OK);
        (await one.ReadRequiredAsync<ManagedAccountResponse>()).Username.Should().Be(created.Account.Username);

        var unknown = await admin.GetAsync($"/v1/admin/accounts/{Guid.NewGuid()}", Ct);

        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Creating_an_account_with_a_taken_username_is_refused()
    {
        using var admin = CreateAdminClient();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(Account.Username, $"{NewUsername()}@example.test"));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "UsernameConflict");
    }

    [Fact]
    public async Task Creating_an_account_with_an_unknown_role_is_not_found()
    {
        using var admin = CreateAdminClient();
        var username = NewUsername();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(username, $"{username}@example.test", Guid.NewGuid()));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "RoleNotFound");
    }

    [Theory]
    [InlineData(Permissions.AccountsManage, "PermissionNotGrantable")]
    [InlineData("items.fly", "UnknownPermission")]
    public async Task Creating_an_account_with_a_permission_that_cannot_be_granted_is_refused(string permission, string error)
    {
        using var admin = CreateAdminClient();
        var username = NewUsername();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(username, $"{username}@example.test", null, [permission]));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, error);
    }

    [Fact]
    public async Task The_admin_cannot_change_their_own_role()
    {
        using var admin = CreateAdminClient();
        var adminId = await GetAccountIdAsync(Factory.AdminAccount!);

        var response = await admin.PutJsonAsync($"/v1/admin/accounts/{adminId}/role", new SetAccountRoleRequest(null));

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "CannotChangeOwnRole");
    }

    [Fact]
    public async Task Setting_an_unknown_role_is_not_found()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync(admin);

        var response = await admin.PutJsonAsync(
            $"/v1/admin/accounts/{created.Account.Id}/role",
            new SetAccountRoleRequest(Guid.NewGuid()));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "RoleNotFound");
    }

    [Fact]
    public async Task A_new_role_reaches_the_member_at_the_next_token_refresh()
    {
        using var admin = CreateAdminClient();
        var (accountId, member) = await CreateMemberAsync(admin);
        var role = await CreateRoleAsync(admin, [.. GrantablePermissions]);

        var response = await admin.PutJsonAsync($"/v1/admin/accounts/{accountId}/role", new SetAccountRoleRequest(role.Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadRequiredAsync<ManagedAccountResponse>()).RoleId.Should().Be(role.Id);

        var token = await RefreshForTokenAsync(member.RefreshToken);

        token.Permissions.Should().Equal(GrantablePermissions);

        using var client = CreateClientFor(member with { AccessToken = token.AccessToken });
        var me = await (await client.GetAsync("/v1/accounts/me", Ct)).ReadRequiredAsync<AccountResponse>();

        me.RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task The_admin_role_given_to_a_member_reaches_the_token_at_the_next_refresh()
    {
        using var admin = CreateAdminClient();
        var (accountId, member) = await CreateMemberAsync(admin);

        var response = await admin.PutJsonAsync($"/v1/admin/accounts/{accountId}/role", new SetAccountRoleRequest(Role.AdminId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await RefreshForTokenAsync(member.RefreshToken);

        token.IsAdmin.Should().BeTrue();
        token.Permissions.Should().Equal(Permissions.All.Select(x => x.Code));
    }

    [Fact]
    public async Task Setting_permissions_without_a_permission_list_is_a_bad_request()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync(admin);

        var response = await admin.PutAsync(
            $"/v1/admin/accounts/{created.Account.Id}/permissions",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Setting_permissions_with_a_null_permission_is_a_bad_request()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync(admin);

        var response = await admin.PutAsync(
            $"/v1/admin/accounts/{created.Account.Id}/permissions",
            new StringContent("{\"permissions\":[null]}", Encoding.UTF8, "application/json"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Creating_an_account_without_a_username_is_a_bad_request()
    {
        using var admin = CreateAdminClient();

        var response = await admin.PostAsync(
            "/v1/admin/accounts",
            new StringContent($"{{\"emailAddress\":\"{NewUsername()}@example.test\"}}", Encoding.UTF8, "application/json"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Setting_an_admin_only_permission_is_refused()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync(admin);

        var response = await admin.PutJsonAsync(
            $"/v1/admin/accounts/{created.Account.Id}/permissions",
            new SetAccountPermissionsRequest([Permissions.AccountsManage]));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "PermissionNotGrantable");
    }

    [Fact]
    public async Task New_grants_reach_the_member_at_the_next_token_refresh()
    {
        using var admin = CreateAdminClient();
        var (accountId, member) = await CreateMemberAsync(admin);

        var response = await admin.PutJsonAsync(
            $"/v1/admin/accounts/{accountId}/permissions",
            new SetAccountPermissionsRequest([.. GrantablePermissions.Reverse()]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadRequiredAsync<ManagedAccountResponse>()).Grants.Should().Equal(GrantablePermissions);

        var token = await RefreshForTokenAsync(member.RefreshToken);

        token.Permissions.Should().Equal(GrantablePermissions);
    }

    [Fact]
    public async Task Revoking_the_sessions_ends_the_refresh_token_of_the_member()
    {
        using var admin = CreateAdminClient();
        var (accountId, member) = await CreateMemberAsync(admin);
        var otherAccount = await CreateBaseAccountAsync();

        var response = await admin.PostAsync($"/v1/admin/accounts/{accountId}/sessions/revoke", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await (await RefreshAsync(member.RefreshToken)).ShouldBeProblemAsync(HttpStatusCode.BadRequest, "InvalidRefreshToken");

        // The revoke ends only the sessions of its own account.
        (await RefreshAsync(otherAccount.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_new_setup_code_replaces_the_old_one()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync(admin);

        var response = await admin.PostAsync($"/v1/admin/accounts/{created.Account.Id}/setup-code", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newCode = await response.ReadRequiredAsync<AccountSetupCodeResponse>();
        newCode.Code.Should().NotBe(created.SetupCode.Code);

        await (await CompleteSetupAsync(created.SetupCode.Code)).ShouldBeProblemAsync(HttpStatusCode.BadRequest, "InvalidSetupCode");

        (await CompleteSetupAsync(newCode.Code)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<RoleResponse> CreateRoleAsync(HttpClient admin, string[] permissions)
    {
        var response = await admin.PostJsonAsync(
            "/v1/admin/roles",
            new SaveRoleRequest($"Role {Guid.NewGuid():N}", permissions));

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<RoleResponse>();
    }

    private async Task<CreatedManagedAccountResponse> CreateManagedAccountAsync(HttpClient admin)
    {
        var username = NewUsername();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(username, $"{username}@example.test"));

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<CreatedManagedAccountResponse>();
    }

    /// <summary>Creates an account without a role, completes its setup and signs it in.</summary>
    private async Task<(Guid AccountId, TestAccount Account)> CreateMemberAsync(HttpClient admin)
    {
        var created = await CreateManagedAccountAsync(admin);

        await (await CompleteSetupAsync(created.SetupCode.Code)).EnsureSuccessAsync();

        var account = await SignInAsync(created.Account.Username, created.Account.EmailAddress, Password);

        return (created.Account.Id, account);
    }

    private Task<HttpResponseMessage> CompleteSetupAsync(string code) =>
        AnonymousClient.PostJsonAsync("/v1/accounts/setup", new CompleteAccountSetupRequest(code, Password));

    private async Task<TokenResponse> RefreshForTokenAsync(string refreshToken)
    {
        var response = await RefreshAsync(refreshToken);

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<TokenResponse>();
    }
}
