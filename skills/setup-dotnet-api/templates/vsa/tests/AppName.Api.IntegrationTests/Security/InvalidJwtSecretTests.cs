// @file-module:auth
namespace AppName.Api.IntegrationTests.Security;

/// <summary>A host with an invalid Jwt:Secret.</summary>
public sealed class InvalidJwtSecretApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["Jwt:Secret"] = "not base64!";
}

public sealed class InvalidJwtSecretTests
{
    /// <summary>
    /// The options are validated before the database step in Program.cs, so an invalid key stops the
    /// server before it changes the database.
    /// </summary>
    [Fact]
    public void The_server_does_not_start_with_an_invalid_key()
    {
        using var factory = new InvalidJwtSecretApiFactory();

        var start = () => factory.Services;

        start.Should().Throw<Exception>().Where(x => x.ToString().Contains("Jwt:Secret must be base64"));
    }
}
