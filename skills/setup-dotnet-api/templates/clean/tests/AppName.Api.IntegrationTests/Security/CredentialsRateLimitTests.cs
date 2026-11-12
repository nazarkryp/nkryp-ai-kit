// @file-module:auth
using System.Net;
using System.Net.Http.Headers;
using AppName.Api.Contracts.Accounts;

namespace AppName.Api.IntegrationTests.Security;

/// <summary>A host that allows three sign-in attempts per client address in each window.</summary>
public sealed class CredentialsRateLimitApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["CredentialsRateLimit:PermitLimit"] = "3";
}

public sealed class CredentialsRateLimitTests(CredentialsRateLimitApiFactory factory)
    : IClassFixture<CredentialsRateLimitApiFactory>
{
    [Fact]
    public async Task Sign_in_attempts_are_limited_per_client_address_and_the_refusal_is_a_problem()
    {
        using var client = factory.CreateClient();
        var responses = new List<HttpResponseMessage>();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            responses.Add(await client.PostJsonAsync(
                "/v1/accounts/signin",
                new SignInRequest { Username = "nobody", Password = "Wrong!Pass1" }));
        }

        responses.Take(3).Select(x => x.StatusCode).Should().NotContain(HttpStatusCode.TooManyRequests);

        var problem = await responses[3].ShouldBeProblemAsync(HttpStatusCode.TooManyRequests, "TooManyRequests");

        problem.Title.Should().Be("Too Many Requests");
        problem.Detail.Should().NotBeNullOrEmpty();
    }
}

/// <summary>
/// A second host with the same limit. The test below needs a limit that no other test has used.
/// </summary>
public sealed class SharedCredentialsRateLimitApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["CredentialsRateLimit:PermitLimit"] = "3";
}

public sealed class SharedCredentialsRateLimitTests(SharedCredentialsRateLimitApiFactory factory)
    : IClassFixture<SharedCredentialsRateLimitApiFactory>
{
    private const string Username = "limitedtester";
    private const string Password = "Sup3r!Secret";

    [Fact]
    public async Task The_routes_that_check_a_password_share_one_limit()
    {
        using var client = factory.CreateClient();

        // The registration has no limit, so it uses none of the three requests.
        var registered = await client.PostJsonAsync(
            "/v1/accounts",
            new CreateAccountRequest { Username = Username, EmailAddress = $"{Username}@example.test", Password = Password });
        await registered.EnsureSuccessAsync();

        var signIn = await client.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = Username, Password = Password });
        await signIn.EnsureSuccessAsync();
        var token = await signIn.ReadRequiredAsync<TokenResponse>();
        // @module:permissions

        var setup = await client.PostJsonAsync(
            "/v1/accounts/setup",
            new CompleteAccountSetupRequest("not a code", "An0ther!Secret"));
        setup.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // @end
        // @module:!permissions

        var wrongSignIn = await client.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = Username, Password = "Wr0ng!Password" });
        wrongSignIn.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // @end

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var change = await client.PutJsonAsync(
            "/v1/accounts/me/password",
            new ChangePasswordRequest("Wr0ng!Password", "N3w!Strong#Pass"));
        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The three requests above used the whole limit, so the fourth one is refused.
        var limited = await client.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = Username, Password = Password });
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
