// @file-module:trust
using AppName.Api.Authentication.Configurations;
using Microsoft.Extensions.Configuration;

namespace AppName.ServiceName.Api.IntegrationTests.Security;

/// <summary>A host without Jwt:Secret and without the key file of the auth service.</summary>
public sealed class NoJwtKeyApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["Jwt:Secret"] = string.Empty;
}

/// <summary>
/// This service checks tokens with the key of the auth service. It reads the key file of the auth service
/// and never makes one: its own key would refuse every token of the auth service.
/// </summary>
public sealed class TrustedJwtSecretTests : IDisposable
{
    private readonly TestPaths _paths = TestPaths.Create();

    private string KeyFile => Path.Combine(_paths.Root, "keys", "jwt-secret");

    public void Dispose() => _paths.Delete();

    [Fact]
    public void Without_a_secret_it_reads_the_key_file_of_the_auth_service()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(KeyFile)!);
        File.WriteAllText(KeyFile, TestConfiguration.JwtSecret + Environment.NewLine);

        var secret = Configuration(secret: null).GetJwtSecret();

        secret.Should().Be(TestConfiguration.JwtSecret);
    }

    [Fact]
    public void A_configured_secret_wins()
    {
        var secret = Configuration(secret: "configured").GetJwtSecret();

        secret.Should().Be("configured");
    }

    [Fact]
    public void Without_a_secret_and_a_key_file_it_fails_and_makes_no_file()
    {
        var act = () => Configuration(secret: null).GetJwtSecret();

        act.Should().Throw<InvalidOperationException>().WithMessage("*does not exist. Start the auth service*");
        File.Exists(KeyFile).Should().BeFalse();
    }

    [Fact]
    public void A_host_without_a_secret_and_a_key_file_does_not_start()
    {
        using var factory = new NoJwtKeyApiFactory();

        var start = () => factory.Services;

        start.Should().Throw<Exception>().Where(x => x.ToString().Contains("Start the auth service of the solution first"));
    }

    private IConfigurationRoot Configuration(string? secret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
                ["Jwt:SecretFile"] = KeyFile,
            })
            .Build();
}
