// @file-module:jwt
namespace AppName.ServiceName.Api.IntegrationTests.Security;

/// <summary>A host with an invalid Jwt:Secret.</summary>
public sealed class InvalidJwtSecretApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["Jwt:Secret"] = "not base64!";
}
// @module:trust

/// <summary>A host without Jwt:Issuer.</summary>
public sealed class NoJwtIssuerApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["Jwt:Issuer"] = string.Empty;
}
// @end

public sealed class InvalidJwtSecretTests
{
    /// <summary>
    // @module:auth
    /// The options are validated before the database step in Program.cs, so an invalid key stops the
    /// server before it changes the database.
    // @end
    // @module:trust
    /// The options are validated at start, so an invalid key stops the server before it serves a request.
    // @end
    /// </summary>
    [Fact]
    public void The_server_does_not_start_with_an_invalid_key()
    {
        using var factory = new InvalidJwtSecretApiFactory();

        var start = () => factory.Services;

        start.Should().Throw<Exception>().Where(x => x.ToString().Contains("Jwt:Secret must be base64"));
    }
    // @module:trust

    /// <summary>Without an issuer, the service would refuse every token of the auth service.</summary>
    [Fact]
    public void The_server_does_not_start_without_an_issuer()
    {
        using var factory = new NoJwtIssuerApiFactory();

        var start = () => factory.Services;

        start.Should().Throw<Exception>().Where(x => x.ToString().Contains("Jwt:Issuer is required"));
    }
    // @end
}
