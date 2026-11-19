using Microsoft.AspNetCore.Mvc.Testing;
// @module:auth
using System.Net.Http.Headers;
using AppName.ServiceName.Api.Features.Accounts.CreateAccount;
using AppName.ServiceName.Api.Features.Accounts.Shared;
using AppName.ServiceName.Api.Features.Accounts.SignIn;
// @module:permissions
using AppName.ServiceName.Api.Features.Admin.Accounts.SetAccountPermissions;
// @end
// @end
// @module:permissions
using AppName.ServiceName.Domain.Accounts;
// @end

namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base for every integration test. It owns the host and hands out HTTP clients.
// @module:auth
/// It also creates the account the tests sign in with.
/// <para>
/// Every helper goes through the HTTP API, so the tests work with any database.
/// </para>
// @end
/// </summary>
public abstract class ApiTestBase : IClassFixture<ApiFactory>, IAsyncLifetime
{
    // @module:auth
    /// <summary>A password that passes every password rule of <see cref="TestConfiguration"/>.</summary>
    protected const string Password = "Sup3r!Secret";

    private static int _accountCounter;

    // @end
    // @module:permissions
    /// <summary>Every permission that an admin can grant.</summary>
    protected static readonly IReadOnlyList<string> GrantablePermissions =
        Permissions.All.Where(x => x.IsGrantable).Select(x => x.Code).ToList();

    // @end
    protected ApiTestBase(ApiFactory factory)
    {
        Factory = factory;
    }

    protected ApiFactory Factory { get; }

    protected TestPaths Paths => Factory.Paths;

    /// <summary>Cancelled when the test run is cancelled or the test times out.</summary>
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A client without a token and without a cookie.</summary>
    protected HttpClient AnonymousClient { get; private set; } = null!;
    // @module:auth

    /// <summary>The account the authenticated client signs in as.</summary>
    protected TestAccount Account { get; private set; } = null!;

    /// <summary>A client with a bearer token for <see cref="Account"/>.</summary>
    protected HttpClient Client { get; private set; } = null!;
    // @end

    public virtual ValueTask InitializeAsync()
    {
        AnonymousClient = CreateCookieFreeClient();
        // @module:!auth

        return ValueTask.CompletedTask;
        // @end
        // @module:auth

        return new ValueTask(SignInTestAccountAsync());
        // @end
    }

    public virtual ValueTask DisposeAsync()
    {
        AnonymousClient?.Dispose();
        // @module:auth
        Client?.Dispose();
        // @end

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A client that does not keep cookies. A test that needs a cookie sends it itself, so it controls
    /// which value arrives.
    /// </summary>
    protected HttpClient CreateCookieFreeClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    // @module:auth

    private async Task SignInTestAccountAsync()
    {
        // @module:permissions
        await Factory.EnsureAdminAsync(RegisterAsync, Ct);

        // @end
        Account = await CreateAccountAsync();
        Client = CreateClientFor(Account);
    }

    // @module:!permissions
    /// <summary>Registers an account and signs it in.</summary>
    protected Task<TestAccount> CreateAccountAsync() => CreateBaseAccountAsync();
    // @end
    // @module:permissions
    /// <summary>
    /// Registers an account with every grantable permission and signs it in. It can use every
    /// feature except the admin-only ones.
    /// </summary>
    protected Task<TestAccount> CreateAccountAsync() => CreateAccountWithPermissionsAsync([.. GrantablePermissions]);

    /// <summary>Registers an account with exactly these permissions, as direct grants, and signs it in.</summary>
    protected async Task<TestAccount> CreateAccountWithPermissionsAsync(params string[] permissions) =>
        await GrantAsync(await CreateBaseAccountAsync(), permissions);
    // @end

    /// <summary>Registers an account with no role and no permission and signs it in.</summary>
    protected async Task<TestAccount> CreateBaseAccountAsync() => (await RegisterAsync()).Account;
    // @module:permissions

    /// <summary>
    /// The admin sets the direct grants of the account through the API, then the account signs in again.
    /// The new access token carries the grants.
    /// </summary>
    protected async Task<TestAccount> GrantAsync(TestAccount account, IReadOnlyCollection<string> permissions)
    {
        if (permissions.Count == 0)
        {
            return account;
        }

        using var admin = CreateAdminClient();

        var accountId = await GetAccountIdAsync(account);
        var response = await admin.PutJsonAsync(
            $"/v1/admin/accounts/{accountId}/permissions",
            new SetAccountPermissionsRequest([.. permissions]));

        await response.EnsureSuccessAsync();

        return await SignInAsync(account.Username, account.EmailAddress, account.Password);
    }

    /// <summary>A client signed in as the admin of this host.</summary>
    protected HttpClient CreateAdminClient() =>
        CreateClientFor(Factory.AdminAccount
            ?? throw new InvalidOperationException("This host has no admin account that the tests created."));
    // @end

    /// <summary>The id of the account, from <c>GET /v1/accounts/me</c>.</summary>
    protected async Task<Guid> GetAccountIdAsync(TestAccount account)
    {
        using var client = CreateClientFor(account);

        var response = await client.GetAsync("/v1/accounts/me", Ct);

        await response.EnsureSuccessAsync();

        return (await response.ReadRequiredAsync<AccountResponse>()).Id;
    }

    /// <summary>Signs in and returns the account with fresh tokens.</summary>
    protected async Task<TestAccount> SignInAsync(string username, string emailAddress, string password)
    {
        var response = await AnonymousClient.PostJsonAsync(
            "/v1/accounts/signin",
            new SignInRequest { Username = username, Password = password });

        await response.EnsureSuccessAsync();

        var token = await response.ReadRequiredAsync<TokenResponse>();

        return new TestAccount(username, emailAddress, password, token.AccessToken, RefreshCookie.ValueOf(response));
    }

    /// <summary>A client with a bearer token for the account. It sends no refresh token cookie.</summary>
    protected HttpClient CreateClientFor(TestAccount account)
    {
        var client = CreateCookieFreeClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

        return client;
    }

    /// <summary>Refreshes with this refresh token in the cookie, like a browser does.</summary>
    protected async Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        using var client = CreateCookieFreeClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts/token/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookie.Name}={refreshToken}");

        return await client.SendAsync(request, Ct);
    }

    /// <summary>A username that passes the username rules and is new on this host.</summary>
    protected static string NewUsername() => $"user{Guid.NewGuid():N}"[..16];

    /// <summary>Registers a new account through the API and signs it in.</summary>
    private async Task<(TestAccount Account, bool IsAdmin)> RegisterAsync()
    {
        var suffix = Interlocked.Increment(ref _accountCounter);
        var username = $"tester{suffix:D4}";
        var emailAddress = $"{username}@example.test";

        var created = await AnonymousClient.PostJsonAsync(
            "/v1/accounts",
            new CreateAccountRequest
            {
                Username = username,
                EmailAddress = emailAddress,
                Password = Password
            });

        await created.EnsureSuccessAsync();

        // @module:permissions
        var isAdmin = (await created.ReadRequiredAsync<AccountResponse>()).IsAdmin;
        // @end
        // @module:!permissions
        const bool isAdmin = false;
        // @end

        return (await SignInAsync(username, emailAddress, Password), isAdmin);
    }
    // @end
}
