// @module:auth
using System.Net;
using System.Net.Http.Json;
using AppName.ServiceName.Api.Features.Accounts.SignIn;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
// @end

namespace AppName.ServiceName.Api.IntegrationTests.Security;
// @module:auth

/// <summary>
/// A host where every request comes from <see cref="ClientAddress"/>, with a limit of three sign-in
/// attempts. TestServer gives no client address, and the forwarded headers middleware trusts a request
/// without one, so the test sets the address first.
/// </summary>
public class DirectClientApiFactory : ApiFactory
{
    public static readonly IPAddress ClientAddress = IPAddress.Parse("203.0.113.7");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
            services.AddSingleton<IStartupFilter>(new ClientAddressStartupFilter(ClientAddress)));
    }

    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["CredentialsRateLimit:PermitLimit"] = "3";

    private sealed class ClientAddressStartupFilter(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;

                return nextMiddleware(context);
            });

            next(app);
        };
    }
}

/// <summary>The same host, where the client address is a configured proxy.</summary>
public sealed class TrustedProxyApiFactory : DirectClientApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings)
    {
        base.ConfigureSettings(settings);
        settings["ForwardedHeaders:KnownProxies:0"] = ClientAddress.ToString();
    }
}

public sealed class DirectClientForwardedHeadersTests(DirectClientApiFactory factory)
    : IClassFixture<DirectClientApiFactory>
{
    [Fact]
    public async Task A_direct_client_cannot_get_a_new_rate_limit_bucket_with_x_forwarded_for()
    {
        var statuses = await SignInFromAsync(factory, "198.51.100.1", "198.51.100.2", "198.51.100.3", "198.51.100.4");

        // The client is not a configured proxy, so its X-Forwarded-For is ignored and all four share a bucket.
        statuses[3].Should().Be(HttpStatusCode.TooManyRequests);
    }

    internal static async Task<List<HttpStatusCode>> SignInFromAsync(ApiFactory host, params string[] forwardedFor)
    {
        using var client = host.CreateClient();
        var statuses = new List<HttpStatusCode>();

        foreach (var address in forwardedFor)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts/signin")
            {
                Content = JsonContent.Create(new SignInRequest { Username = "nobody", Password = "Wrong!Pass1" })
            };
            request.Headers.Add("X-Forwarded-For", address);

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            statuses.Add(response.StatusCode);
        }

        return statuses;
    }
}

public sealed class TrustedProxyForwardedHeadersTests(TrustedProxyApiFactory factory)
    : IClassFixture<TrustedProxyApiFactory>
{
    [Fact]
    public async Task A_configured_proxy_sets_the_client_address()
    {
        var statuses = await DirectClientForwardedHeadersTests.SignInFromAsync(
            factory, "198.51.100.1", "198.51.100.2", "198.51.100.3", "198.51.100.4");

        // Each forwarded client address has its own bucket, so no request reaches the limit.
        statuses.Should().NotContain(HttpStatusCode.TooManyRequests);
    }
}
// @end

/// <summary>A host with a value in ForwardedHeaders:KnownNetworks that is not a network.</summary>
public sealed class InvalidTrustedNetworkApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["ForwardedHeaders:KnownNetworks:0"] = "not-a-network";
}

public sealed class InvalidTrustedNetworkTests
{
    [Fact]
    public void The_server_does_not_start_with_an_invalid_proxy_network()
    {
        using var factory = new InvalidTrustedNetworkApiFactory();

        var start = () => factory.Services;

        start.Should().Throw<Exception>().Where(x => x.ToString().Contains("ForwardedHeaders:KnownNetworks"));
    }
}
