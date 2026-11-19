// @file-module:trust
using System.Net;
using System.Net.Http.Headers;
using AppName.ServiceName.Api.Features.Me;

namespace AppName.ServiceName.Api.IntegrationTests.Me;

/// <summary>
/// The service accepts an access token of the auth service and refuses every other token.
/// </summary>
public sealed class MeEndpointTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string MeRoute = "/v1/me";

    [Fact]
    public async Task Me_without_a_token_is_unauthorized()
    {
        var response = await AnonymousClient.GetAsync(MeRoute, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_an_access_token_of_the_auth_service_returns_the_user()
    {
        var userId = Guid.NewGuid();
        var token = TestTokens.Create(userId, "jane.doe", "jane@example.com");

        var response = await GetMeAsync(token);

        await response.EnsureSuccessAsync();
        var me = await response.ReadRequiredAsync<MeResponse>();

        me.Should().Be(new MeResponse(userId, "jane.doe", "jane@example.com"));
    }

    [Fact]
    public async Task Me_with_a_token_signed_with_another_key_is_unauthorized()
    {
        var token = TestTokens.Create(Guid.NewGuid(), "jane.doe", "jane@example.com",
            secret: TestTokens.CreateOtherSecret());

        var response = await GetMeAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_a_token_that_is_not_an_access_token_is_unauthorized()
    {
        var token = TestTokens.Create(Guid.NewGuid(), "jane.doe", "jane@example.com", type: "JWT");

        var response = await GetMeAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> GetMeAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MeRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await AnonymousClient.SendAsync(request, Ct);
    }
}
