// @file-module:trust
using System.Net;
using System.Net.Http.Headers;
using AppName.ServiceName.Api.Contracts.Me;

namespace AppName.ServiceName.Api.IntegrationTests.Me;

/// <summary>
/// This service has no accounts. It accepts only the access tokens of the auth service of the solution:
/// the same key, issuer, audience and token type.
/// </summary>
public sealed class MeEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task A_request_without_a_token_gets_401()
    {
        var response = await AnonymousClient.GetAsync("/v1/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_of_the_auth_service_gets_the_user_of_its_claims()
    {
        using var client = ClientWith(TestTokens.Create());

        var response = await client.GetAsync("/v1/me", Ct);

        await response.EnsureSuccessAsync();

        var me = await response.ReadRequiredAsync<MeResponse>();

        me.UserId.Should().Be(TestTokens.UserId);
        me.Username.Should().Be(TestTokens.Username);
        me.EmailAddress.Should().Be(TestTokens.EmailAddress);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_gets_401()
    {
        using var client = ClientWith(TestTokens.Create(secret: TestTokens.OtherSecret()));

        var response = await client.GetAsync("/v1/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_of_another_type_gets_401()
    {
        using var client = ClientWith(TestTokens.Create(type: "JWT"));

        var response = await client.GetAsync("/v1/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient ClientWith(string accessToken)
    {
        var client = CreateCookieFreeClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }
}
