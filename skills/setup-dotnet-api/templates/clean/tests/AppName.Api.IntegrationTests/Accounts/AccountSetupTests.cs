// @file-module:permissions
using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AppName.Api.Contracts.Accounts;

namespace AppName.Api.IntegrationTests.Accounts;

/// <summary>The user sets a password with the one-time setup code from the admin (anonymous <c>POST /v1/accounts/setup</c>).</summary>
public sealed class AccountSetupTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task A_valid_code_sets_the_password_and_the_account_can_sign_in()
    {
        var created = await CreateManagedAccountAsync();

        var response = await CompleteSetupAsync(created.SetupCode.Code, Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadRequiredAsync<AccountResponse>()).Username.Should().Be(created.Account.Username);

        var signIn = await SignInWithAsync(created.Account.Username, Password);

        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_code_works_only_once()
    {
        var created = await CreateManagedAccountAsync();

        await (await CompleteSetupAsync(created.SetupCode.Code, Password)).EnsureSuccessAsync();

        var second = await CompleteSetupAsync(created.SetupCode.Code, "An0ther!Secret");

        await second.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "InvalidSetupCode");
    }

    [Fact]
    public async Task Two_setups_with_one_code_at_the_same_time_succeed_only_once()
    {
        var created = await CreateManagedAccountAsync();

        var responses = await Task.WhenAll(
            CompleteSetupAsync(created.SetupCode.Code, Password),
            CompleteSetupAsync(created.SetupCode.Code, "An0ther!Secret"));

        responses.Count(x => x.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
    }

    [Fact]
    public async Task A_malformed_code_and_an_unknown_code_are_refused_the_same_way()
    {
        var unknown = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));

        await (await CompleteSetupAsync("not a code", Password)).ShouldBeProblemAsync(HttpStatusCode.BadRequest, "InvalidSetupCode");
        await (await CompleteSetupAsync(unknown, Password)).ShouldBeProblemAsync(HttpStatusCode.BadRequest, "InvalidSetupCode");
    }

    [Fact]
    public async Task A_weak_password_is_refused_and_the_code_still_works()
    {
        var created = await CreateManagedAccountAsync();

        // Only the special character rule fails, so the error has the code of that rule.
        var weak = await CompleteSetupAsync(created.SetupCode.Code, "Sup3rSecret");

        await weak.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "PasswordRequiresSpecialCharacter");

        (await CompleteSetupAsync(created.SetupCode.Code, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_password_that_contains_the_username_is_refused()
    {
        var created = await CreateManagedAccountAsync();

        var response = await CompleteSetupAsync(created.SetupCode.Code, $"X1!{created.Account.Username}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "PasswordContainsUsername");
    }

    [Fact]
    public async Task The_setup_ends_every_session_of_the_account()
    {
        using var admin = CreateAdminClient();
        var created = await CreateManagedAccountAsync();
        await (await CompleteSetupAsync(created.SetupCode.Code, Password)).EnsureSuccessAsync();

        var oldSession = await SignInAsync(created.Account.Username, created.Account.EmailAddress, Password);

        var issued = await admin.PostAsync($"/v1/admin/accounts/{created.Account.Id}/setup-code", null, Ct);
        await issued.EnsureSuccessAsync();
        var newCode = await issued.ReadRequiredAsync<AccountSetupCodeResponse>();

        var otherAccount = await CreateBaseAccountAsync();

        await (await CompleteSetupAsync(newCode.Code, "An0ther!Secret")).EnsureSuccessAsync();

        (await RefreshAsync(oldSession.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The setup ends only the sessions of its own account.
        (await RefreshAsync(otherAccount.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// A username without a run of four equal characters. Such a run breaks one more password rule, and then
    /// the error is ValidationFailed instead of the code of the rule under test.
    /// </summary>
    private static string NewUsernameWithoutRuns()
    {
        while (true)
        {
            var username = NewUsername();

            if (!Regex.IsMatch(username, @"(.)\1{3}"))
            {
                return username;
            }
        }
    }

    private async Task<CreatedManagedAccountResponse> CreateManagedAccountAsync()
    {
        using var admin = CreateAdminClient();
        var username = NewUsernameWithoutRuns();

        var response = await admin.PostJsonAsync(
            "/v1/admin/accounts",
            new CreateManagedAccountRequest(username, $"{username}@example.test"));

        await response.EnsureSuccessAsync();

        return await response.ReadRequiredAsync<CreatedManagedAccountResponse>();
    }

    private Task<HttpResponseMessage> CompleteSetupAsync(string code, string password) =>
        AnonymousClient.PostJsonAsync("/v1/accounts/setup", new CompleteAccountSetupRequest(code, password));

    private Task<HttpResponseMessage> SignInWithAsync(string username, string password) =>
        AnonymousClient.PostJsonAsync("/v1/accounts/signin", new SignInRequest { Username = username, Password = password });
}
