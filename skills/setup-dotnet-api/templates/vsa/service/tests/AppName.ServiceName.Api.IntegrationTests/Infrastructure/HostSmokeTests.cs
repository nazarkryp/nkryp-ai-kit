using System.Net;
using System.Text.Json;

namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

public sealed class HostSmokeTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Health_endpoint_reports_healthy_to_an_anonymous_caller()
    {
        var response = await AnonymousClient.GetAsync("/health", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).Should().Contain("Healthy");
    }

    [Fact]
    public async Task OpenApi_document_is_served_to_an_anonymous_caller()
    {
        var response = await AnonymousClient.GetAsync("/openapi/v1.json", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

        document.RootElement.GetProperty("openapi").GetString().Should().NotBeNullOrEmpty();
    }
}
