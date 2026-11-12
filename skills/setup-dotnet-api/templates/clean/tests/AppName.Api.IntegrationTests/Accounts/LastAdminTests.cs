// @file-module:permissions
using System.Net;
using AppName.Api.Contracts.Accounts;
using AppName.Api.Contracts.Roles;
using AppName.Application.Common;

namespace AppName.Api.IntegrationTests.Accounts;

/// <summary>
/// The last admin keeps the Admin role, also when two admins remove the role of each other at the same time.
/// This class has its own host, because the test demotes an admin.
/// </summary>
public sealed class LastAdminTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Two_admins_who_demote_each_other_at_the_same_time_leave_exactly_one_admin()
    {
        var hostAdmin = Factory.AdminAccount!;
        using var hostAdminClient = CreateClientFor(
            await SignInAsync(hostAdmin.Username, hostAdmin.EmailAddress, hostAdmin.Password));

        var hostAdminId = await GetAccountIdAsync(hostAdmin);

        var (secondAdminId, secondAdmin) = await CreateSecondAdminAsync(hostAdminClient);
        using var secondAdminClient = CreateClientFor(secondAdmin);

        var responses = await Task.WhenAll(
            hostAdminClient.PutJsonAsync($"/v1/admin/accounts/{secondAdminId}/role", new SetAccountRoleRequest(null)),
            secondAdminClient.PutJsonAsync($"/v1/admin/accounts/{hostAdminId}/role", new SetAccountRoleRequest(null)));

        responses.Count(x => x.StatusCode == HttpStatusCode.OK).Should().Be(1);
        var refused = responses.Should().ContainSingle(x => x.StatusCode == HttpStatusCode.Conflict).Subject;
        await refused.ShouldBeProblemAsync(HttpStatusCode.Conflict, "LastAdmin");

        // The admin whose request succeeded is the admin that is left.
        var remainingClient = responses[0].StatusCode == HttpStatusCode.OK ? hostAdminClient : secondAdminClient;
        var list = await remainingClient.GetAsync("/v1/admin/accounts", Ct);
        await list.EnsureSuccessAsync();

        (await list.ReadRequiredAsync<CollectionResponse<ManagedAccountResponse>>()).Data
            .Count(x => x.IsAdmin).Should().Be(1);
    }

    /// <summary>The host admin creates an account with the Admin role, which completes its setup and signs in.</summary>
    private async Task<(Guid AccountId, TestAccount Account)> CreateSecondAdminAsync(HttpClient hostAdminClient)
    {
        var roles = await hostAdminClient.GetAsync("/v1/admin/roles", Ct);
        await roles.EnsureSuccessAsync();
        var adminRoleId = (await roles.ReadRequiredAsync<CollectionResponse<RoleResponse>>()).Data.Single(x => x.IsAdmin).Id;

        var username = NewUsername();

        var created = await hostAdminClient.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(username, $"{username}@example.test", adminRoleId));

        await created.EnsureSuccessAsync();
        var account = await created.ReadRequiredAsync<CreatedManagedAccountResponse>();
        account.Account.IsAdmin.Should().BeTrue();

        var setup = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/setup",
            new CompleteAccountSetupRequest(account.SetupCode.Code, Password));

        await setup.EnsureSuccessAsync();

        return (account.Account.Id, await SignInAsync(username, account.Account.EmailAddress, Password));
    }
}
