// @file-module:auth
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AppName.ServiceName.Api.Features.Accounts.CreateAccount;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Features.Accounts.SignIn;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>
/// Registration, sign in, token refresh and sign out, driven through the real endpoints.
/// </summary>
public sealed class AccountsEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_account_returns_created()
    {
        var username = NewUsername();

        var response = await AnonymousClient.PostJsonAsync("/v1/accounts", NewAccount(username));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var account = await response.ReadRequiredAsync<AccountResponse>();

        account.Id.Should().NotBeEmpty();
        account.Username.Should().Be(username);
        account.EmailAddress.Should().Be($"{username}@example.test");
    }

    [Fact]
    public async Task Create_account_rejects_a_weak_password()
    {
        var response = await AnonymousClient.PostJsonAsync("/v1/accounts", NewAccount(NewUsername(), password: "short"));

        await AssertBadRequestAsync(response);
    }

    [Fact]
    public async Task Create_account_rejects_a_duplicate_username()
    {
        var username = NewUsername();

        var first = await AnonymousClient.PostJsonAsync("/v1/accounts", NewAccount(username));
        await first.EnsureSuccessAsync();

        // The comparison ignores letter case.
        var second = await AnonymousClient.PostJsonAsync("/v1/accounts", NewAccount(username.ToUpperInvariant()));

        var problem = await AssertBadRequestAsync(second);

        problem.Type.Should().Be("UsernameConflict");
    }

    [Fact]
    public async Task Parallel_registrations_of_one_username_create_one_account()
    {
        var username = NewUsername();

        // Both requests pass the name check before either one inserts. The insert decides the race.
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(i => AnonymousClient.PostJsonAsync(
                "/v1/accounts",
                NewAccount(i % 2 == 0 ? username : username.ToUpperInvariant()))));

        responses.Count(x => x.StatusCode == HttpStatusCode.Created).Should().Be(1);

        foreach (var response in responses.Where(x => x.StatusCode != HttpStatusCode.Created))
        {
            (await AssertBadRequestAsync(response)).Type.Should().Be("UsernameConflict");
        }
    }

    [Fact]
    public async Task Create_account_with_missing_fields_returns_bad_request()
    {
        // The rules stop at the first failure of each field, so a missing value never reaches the later checks.
        var response = await AnonymousClient.PostJsonAsync("/v1/accounts", new { });

        var problem = await AssertBadRequestAsync(response);

        problem.Extensions.Should().ContainKey("details");
        problem.Extensions["details"]!.ToString().Should().Contain("Username");
    }

    [Fact]
    public async Task Create_account_rejects_an_invalid_username()
    {
        var response = await AnonymousClient.PostJsonAsync("/v1/accounts", NewAccount("_not a name!"));

        await AssertBadRequestAsync(response);
    }

    [Fact]
    public async Task Sign_in_returns_an_access_token_and_puts_the_refresh_token_only_in_the_cookie()
    {
        var response = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = Account.Username, Password = Account.Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.ReadRequiredAsync<TokenResponse>();

        token.TokenType.Should().Be("Bearer");
        token.AccessToken.Should().NotBeNullOrEmpty();
        token.ExpiresIn.Should().BeGreaterThan(0);

        await ShouldHaveNoRefreshTokenInTheBodyAsync(response);
        ShouldSetAStrictHttpOnlyCookie(response);
    }

    [Fact]
    public async Task Sign_in_with_the_wrong_password_fails()
    {
        var response = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = Account.Username, Password = "Wr0ng!Password" });

        var problem = await AssertBadRequestAsync(response);

        problem.Type.Should().Be("InvalidCredentials");
        RefreshCookie.SetCookieHeader(response).Should().BeNull();
    }

    [Fact]
    public async Task Sign_in_with_an_unknown_username_fails_the_same_way()
    {
        var response = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = NewUsername(), Password = Password });

        var problem = await AssertBadRequestAsync(response);

        problem.Type.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task Get_me_returns_the_caller()
    {
        var response = await Client.GetAsync("/v1/accounts/me", Ct);

        await response.EnsureSuccessAsync();

        var account = await response.ReadRequiredAsync<AccountResponse>();

        account.Username.Should().Be(Account.Username);
        account.EmailAddress.Should().Be(Account.EmailAddress);
    }

    [Fact]
    public async Task Refresh_rotates_the_refresh_token_cookie_and_retires_the_old_one()
    {
        var refreshed = await RefreshAsync(Account.RefreshToken);

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await refreshed.ReadRequiredAsync<TokenResponse>();
        var rotated = RefreshCookie.ValueOf(refreshed);

        token.AccessToken.Should().NotBeNullOrEmpty();
        rotated.Should().NotBe(Account.RefreshToken);
        await ShouldHaveNoRefreshTokenInTheBodyAsync(refreshed);
        ShouldSetAStrictHttpOnlyCookie(refreshed);

        var reused = await RefreshAsync(Account.RefreshToken);

        var problem = await AssertBadRequestAsync(reused);

        problem.Type.Should().Be("InvalidRefreshToken");

        // The rotated token is the one that works now.
        var again = await RefreshAsync(rotated);

        again.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_new_access_token_from_a_refresh_works()
    {
        var refreshed = await RefreshAsync(Account.RefreshToken);
        await refreshed.EnsureSuccessAsync();
        var token = await refreshed.ReadRequiredAsync<TokenResponse>();

        using var client = CreateClientFor(Account with { AccessToken = token.AccessToken });

        (await client.GetAsync("/v1/accounts/me", Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_browser_client_refreshes_with_the_cookie_that_the_sign_in_set()
    {
        // This client keeps cookies, like a browser. It never sends the refresh token itself.
        using var browser = Factory.CreateClient();

        var signIn = await browser.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = Account.Username, Password = Account.Password });
        await signIn.EnsureSuccessAsync();

        // Each refresh replaces the cookie, so the second refresh sends the rotated token.
        (await browser.PostAsync("/v1/accounts/token/refresh", null, Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await browser.PostAsync("/v1/accounts/token/refresh", null, Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_without_a_cookie_returns_unauthorized()
    {
        var response = await AnonymousClient.PostAsync("/v1/accounts/token/refresh", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ignores_a_refresh_token_in_the_body()
    {
        var body = JsonSerializer.Serialize(new { refresh_token = Account.RefreshToken, refreshToken = Account.RefreshToken });

        var response = await AnonymousClient.PostAsync(
            "/v1/accounts/token/refresh",
            new StringContent(body, Encoding.UTF8, "application/json"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The token was not used, so the cookie still works.
        (await RefreshAsync(Account.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_with_an_unknown_token_fails()
    {
        var response = await RefreshAsync(Guid.NewGuid().ToString("N"));

        var problem = await AssertBadRequestAsync(response);

        problem.Type.Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Two_refreshes_with_one_refresh_token_give_new_tokens_only_once()
    {
        var account = await CreateBaseAccountAsync();

        var responses = await Task.WhenAll(RefreshAsync(account.RefreshToken), RefreshAsync(account.RefreshToken));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
    }

    [Fact]
    public async Task Sign_out_revokes_the_access_token_and_the_refresh_token_and_clears_the_cookie()
    {
        var account = await CreateBaseAccountAsync();

        using var client = CreateClientFor(account);

        var before = await client.GetAsync("/v1/accounts/me", Ct);
        before.StatusCode.Should().Be(HttpStatusCode.OK);

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts/signout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
        request.Headers.Add("Cookie", $"{RefreshCookie.Name}={account.RefreshToken}");

        var signedOut = await AnonymousClient.SendAsync(request, Ct);
        signedOut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The cookie is cleared: an empty value with an expiry in the past.
        var cleared = RefreshCookie.SetCookieHeader(signedOut);
        cleared.Should().NotBeNull();
        RefreshCookie.ValueOf(signedOut).Should().BeEmpty();
        cleared.Should().Contain("expires=Thu, 01 Jan 1970");

        var after = await client.GetAsync("/v1/accounts/me", Ct);
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var refresh = await RefreshAsync(account.RefreshToken);
        (await AssertBadRequestAsync(refresh)).Type.Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Sign_out_without_a_token_is_unauthorized()
    {
        var response = await AnonymousClient.PostAsync("/v1/accounts/signout", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void ShouldSetAStrictHttpOnlyCookie(HttpResponseMessage response)
    {
        var header = RefreshCookie.SetCookieHeader(response);

        header.Should().NotBeNull();

        var attributes = header!.Split(';').Skip(1).Select(x => x.Trim().ToLowerInvariant()).ToList();

        attributes.Should().Contain("httponly");
        attributes.Should().Contain("samesite=strict");
        attributes.Should().Contain(x => x.StartsWith("expires=", StringComparison.Ordinal));
    }

    private static async Task ShouldHaveNoRefreshTokenInTheBodyAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(Ct);
        using var document = JsonDocument.Parse(body);

        document.RootElement.EnumerateObject().Select(x => x.Name)
            .Should().NotContain(name => name.Contains("refresh", StringComparison.OrdinalIgnoreCase));
        body.Should().NotContain(RefreshCookie.ValueOf(response));
    }

    private static async Task<ProblemDetails> AssertBadRequestAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.ReadRequiredAsync<ProblemDetails>();

        problem.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Detail.Should().NotBeNullOrEmpty();

        return problem;
    }

    private static CreateAccountRequest NewAccount(string username, string? password = null) => new()
    {
        Username = username,
        EmailAddress = $"{username}@example.test",
        Password = password ?? Password
    };
}
