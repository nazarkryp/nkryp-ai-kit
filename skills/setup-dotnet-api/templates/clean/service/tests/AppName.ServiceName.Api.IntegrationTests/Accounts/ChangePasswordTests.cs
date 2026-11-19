// @file-module:auth
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppName.ServiceName.Api.Contracts.Accounts;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>The signed-in user changes their password (<c>PUT /v1/accounts/me/password</c>).</summary>
public sealed class ChangePasswordTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string NewPassword = "N3w!Strong#Pass";

    [Fact]
    public async Task A_wrong_current_password_is_refused()
    {
        var account = await CreateBaseAccountAsync();

        var response = await ChangePasswordAsync(account, "Wr0ng!Password", NewPassword);

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "CurrentPasswordInvalid");
    }

    [Fact]
    public async Task A_weak_new_password_is_refused()
    {
        var account = await CreateBaseAccountAsync();

        // Only the uppercase rule fails, so the error has the code of that rule.
        var response = await ChangePasswordAsync(account, account.Password, "n3w!strong#pass");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "PasswordRequiresUppercase");
    }

    [Fact]
    public async Task The_new_password_replaces_the_old_one_and_only_the_current_session_stays()
    {
        var account = await CreateBaseAccountAsync();
        var otherSession = await SignInAsync(account.Username, account.EmailAddress, account.Password);

        var response = await ChangePasswordAsync(account, account.Password, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await SignInWithAsync(account.Username, account.Password)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SignInWithAsync(account.Username, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await RefreshAsync(otherSession.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await RefreshAsync(account.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Without_the_refresh_token_cookie_every_session_ends()
    {
        var account = await CreateBaseAccountAsync();

        using var client = CreateClientFor(account);
        var response = await client.PutJsonAsync("/v1/accounts/me/password", new ChangePasswordRequest(account.Password, NewPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RefreshAsync(account.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    // @module:permissions

    [Fact]
    public async Task A_password_change_cancels_the_open_setup_code()
    {
        using var admin = CreateAdminClient();
        var account = await CreateBaseAccountAsync();
        var accountId = await GetAccountIdAsync(account);

        var issued = await admin.PostAsync($"/v1/admin/accounts/{accountId}/setup-code", null, Ct);
        await issued.EnsureSuccessAsync();
        var setupCode = await issued.ReadRequiredAsync<AccountSetupCodeResponse>();

        (await ChangePasswordAsync(account, account.Password, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);

        var setup = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/setup",
            new CompleteAccountSetupRequest(setupCode.Code, "An0ther!Secret"));

        await setup.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "InvalidSetupCode");

        var managed = await admin.GetAsync($"/v1/admin/accounts/{accountId}", Ct);
        await managed.EnsureSuccessAsync();

        (await managed.ReadRequiredAsync<ManagedAccountResponse>()).SetupCodeExpiresAt.Should().BeNull();
    }
    // @end

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        var response = await AnonymousClient.PutJsonAsync(
            "/v1/accounts/me/password",
            new ChangePasswordRequest(Password, NewPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Sends the access token and the refresh token cookie of the account, like the browser does.</summary>
    private async Task<HttpResponseMessage> ChangePasswordAsync(TestAccount account, string currentPassword, string newPassword)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/v1/accounts/me/password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(currentPassword, newPassword), options: TestJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
        request.Headers.Add("Cookie", $"{RefreshCookie.Name}={account.RefreshToken}");

        return await AnonymousClient.SendAsync(request, Ct);
    }

    private Task<HttpResponseMessage> SignInWithAsync(string username, string password) =>
        AnonymousClient.PostJsonAsync("/v1/accounts/signin", new SignInRequest { Username = username, Password = password });
}
