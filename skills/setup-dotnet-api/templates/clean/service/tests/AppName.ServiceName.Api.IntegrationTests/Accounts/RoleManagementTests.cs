// @file-module:permissions
using System.Net;
using System.Text;
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.ServiceName.Api.Contracts.Roles;
using AppName.ServiceName.Application.Common;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>The admin manages roles through <c>/v1/admin/roles</c> and reads the permission catalog.</summary>
public sealed class RoleManagementTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task The_Admin_role_comes_first_and_has_every_permission()
    {
        using var admin = CreateAdminClient();
        await CreateRoleAsync(admin, []);

        var response = await admin.GetAsync("/v1/admin/roles", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = (await response.ReadRequiredAsync<CollectionResponse<RoleResponse>>()).Data;
        roles.Should().HaveCountGreaterThan(1);
        roles[0].IsAdmin.Should().BeTrue();
        roles[0].Id.Should().Be(Role.AdminId);
        roles[0].Name.Should().Be(Role.AdminName);
        roles[0].Permissions.Should().Equal(Permissions.All.Select(x => x.Code));
        roles[0].AccountCount.Should().Be(1);
    }

    [Fact]
    public async Task The_admin_creates_a_role()
    {
        using var admin = CreateAdminClient();
        var name = NewRoleName();

        var response = await admin.PostJsonAsync(
            "/v1/admin/roles",
            new SaveRoleRequest(name, [.. GrantablePermissions.Reverse()]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var role = await response.ReadRequiredAsync<RoleResponse>();
        response.Headers.Location!.OriginalString.Should().Be($"/v1/admin/roles/{role.Id}");
        role.Name.Should().Be(name);
        role.IsAdmin.Should().BeFalse();
        role.Permissions.Should().Equal(GrantablePermissions);
        role.AccountCount.Should().Be(0);

        var get = await admin.GetAsync($"/v1/admin/roles/{role.Id}", Ct);

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.ReadRequiredAsync<RoleResponse>()).Name.Should().Be(name);
    }

    [Fact]
    public async Task A_role_name_that_differs_only_in_letter_case_is_a_conflict()
    {
        using var admin = CreateAdminClient();
        var role = await CreateRoleAsync(admin, []);

        var response = await admin.PostJsonAsync(
            "/v1/admin/roles",
            new SaveRoleRequest(role.Name.ToUpperInvariant(), []));

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "RoleNameConflict");
    }

    [Fact]
    public async Task Renaming_a_role_to_the_name_of_another_role_is_a_conflict()
    {
        using var admin = CreateAdminClient();
        var first = await CreateRoleAsync(admin, []);
        var second = await CreateRoleAsync(admin, []);

        var response = await admin.PutJsonAsync($"/v1/admin/roles/{second.Id}", new SaveRoleRequest(first.Name, []));

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "RoleNameConflict");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_role_name_is_refused(string name)
    {
        using var admin = CreateAdminClient();

        var response = await admin.PostJsonAsync("/v1/admin/roles", new SaveRoleRequest(name, []));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "RoleNameRequired");
    }

    [Fact]
    public async Task A_role_name_longer_than_64_characters_is_refused()
    {
        using var admin = CreateAdminClient();

        var response = await admin.PostJsonAsync("/v1/admin/roles", new SaveRoleRequest(new string('r', 65), []));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "RoleNameTooLong");
    }

    [Theory]
    [InlineData(Permissions.AccountsManage, "PermissionNotGrantable")]
    [InlineData("items.fly", "UnknownPermission")]
    public async Task A_role_with_a_permission_that_cannot_be_granted_is_refused(string permission, string error)
    {
        using var admin = CreateAdminClient();

        var response = await admin.PostJsonAsync("/v1/admin/roles", new SaveRoleRequest(NewRoleName(), [permission]));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, error);
    }

    [Fact]
    public async Task A_role_without_a_permission_list_is_a_bad_request()
    {
        using var admin = CreateAdminClient();

        var response = await admin.PostAsync(
            "/v1/admin/roles",
            new StringContent($$"""{"name":"{{NewRoleName()}}"}""", Encoding.UTF8, "application/json"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_updated_role_reaches_its_members_at_the_next_token_refresh()
    {
        using var admin = CreateAdminClient();
        var role = await CreateRoleAsync(admin, []);
        var member = await CreateMemberAsync(admin, role.Id);
        var newName = NewRoleName();

        var response = await admin.PutJsonAsync(
            $"/v1/admin/roles/{role.Id}",
            new SaveRoleRequest(newName, [.. GrantablePermissions]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadRequiredAsync<RoleResponse>();
        updated.Name.Should().Be(newName);
        updated.Permissions.Should().Equal(GrantablePermissions);
        updated.AccountCount.Should().Be(1);

        var refresh = await RefreshAsync(member.RefreshToken);
        await refresh.EnsureSuccessAsync();
        var token = await refresh.ReadRequiredAsync<TokenResponse>();

        token.Permissions.Should().Equal(GrantablePermissions);

        using var client = CreateClientFor(member with { AccessToken = token.AccessToken });
        (await (await client.GetAsync("/v1/accounts/me", Ct)).ReadRequiredAsync<AccountResponse>())
            .RoleName.Should().Be(newName);
    }

    [Fact]
    public async Task Updating_an_unknown_role_is_not_found()
    {
        using var admin = CreateAdminClient();

        var response = await admin.PutJsonAsync($"/v1/admin/roles/{Guid.NewGuid()}", new SaveRoleRequest(NewRoleName(), []));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "RoleNotFound");
    }

    [Fact]
    public async Task Nobody_can_update_or_delete_the_Admin_role()
    {
        using var admin = CreateAdminClient();

        var update = await admin.PutJsonAsync($"/v1/admin/roles/{Role.AdminId}", new SaveRoleRequest(NewRoleName(), []));

        await update.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "AdminRoleLocked");

        var delete = await admin.DeleteAsync($"/v1/admin/roles/{Role.AdminId}", Ct);

        await delete.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "AdminRoleLocked");
    }

    [Fact]
    public async Task A_role_that_an_account_has_cannot_be_deleted()
    {
        using var admin = CreateAdminClient();
        var role = await CreateRoleAsync(admin, []);
        await CreateManagedAccountAsync(admin, role.Id);

        var response = await admin.DeleteAsync($"/v1/admin/roles/{role.Id}", Ct);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "RoleInUse");
    }

    [Fact]
    public async Task An_unused_role_is_deleted()
    {
        using var admin = CreateAdminClient();
        var role = await CreateRoleAsync(admin, []);

        var response = await admin.DeleteAsync($"/v1/admin/roles/{role.Id}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await admin.GetAsync($"/v1/admin/roles/{role.Id}", Ct);

        await get.ShouldBeProblemAsync(HttpStatusCode.NotFound, "RoleNotFound");
    }

    [Fact]
    public async Task The_permission_catalog_lists_every_code_with_its_description_and_grantable_flag()
    {
        using var admin = CreateAdminClient();

        var response = await admin.GetAsync("/v1/admin/permissions", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = (await response.ReadRequiredAsync<CollectionResponse<PermissionDefinitionResponse>>()).Data;

        catalog.Should().Equal(
            Permissions.All.Select(x => new PermissionDefinitionResponse(x.Code, x.Description, x.IsGrantable)));
        catalog.Should().Contain(x => x.Code == Permissions.AccountsManage && !x.IsGrantable);
    }

    [Fact]
    public async Task An_account_that_is_not_an_admin_cannot_create_a_role()
    {
        var response = await Client.PostJsonAsync("/v1/admin/roles", new SaveRoleRequest(NewRoleName(), []));

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "InsufficientPermissions");
    }

    private static string NewRoleName() => $"Role {Guid.NewGuid():N}";

    private async Task<RoleResponse> CreateRoleAsync(HttpClient admin, string[] permissions)
    {
        var response = await admin.PostJsonAsync("/v1/admin/roles", new SaveRoleRequest(NewRoleName(), permissions));

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<RoleResponse>();
    }

    private async Task<CreatedManagedAccountResponse> CreateManagedAccountAsync(HttpClient admin, Guid roleId)
    {
        var username = NewUsername();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(username, $"{username}@example.test", roleId));

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<CreatedManagedAccountResponse>();
    }

    /// <summary>Creates an account with the role, completes its setup and signs it in.</summary>
    private async Task<TestAccount> CreateMemberAsync(HttpClient admin, Guid roleId)
    {
        var created = await CreateManagedAccountAsync(admin, roleId);

        var setup = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/setup",
            new CompleteAccountSetupRequest(created.SetupCode.Code, Password));

        await setup.EnsureSuccessAsync();

        return await SignInAsync(created.Account.Username, created.Account.EmailAddress, Password);
    }
}
