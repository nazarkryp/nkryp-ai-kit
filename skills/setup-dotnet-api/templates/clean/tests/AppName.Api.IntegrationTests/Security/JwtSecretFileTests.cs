// @file-module:auth
using AppName.Api.Infrastructure.Configurations;
using AppName.Security.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AppName.Api.IntegrationTests.Security;

/// <summary>A host without Jwt:Secret. The server makes the key and saves it in Jwt:SecretFile.</summary>
public sealed class NoJwtSecretApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["Jwt:Secret"] = string.Empty;
}

public sealed class JwtSecretFileTests : IDisposable
{
    private readonly TestPaths _paths = TestPaths.Create();

    private string KeyFile => Path.Combine(_paths.Root, "keys", "jwt-secret");

    public void Dispose() => _paths.Delete();

    [Fact]
    public void Without_a_secret_it_makes_and_saves_a_key()
    {
        var secret = Configuration(secret: null).GetOrCreateJwtSecret();

        secret.Should().Be(File.ReadAllText(KeyFile));
        Convert.FromBase64String(secret).Should().HaveCount(32);
    }

    [Fact]
    public void Only_the_owner_can_read_the_key_file()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows has no Unix file mode.");
            return;
        }

        Configuration(secret: null).GetOrCreateJwtSecret();

        File.GetUnixFileMode(KeyFile).Should().Be(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    public void A_later_start_uses_the_saved_key()
    {
        var first = Configuration(secret: null).GetOrCreateJwtSecret();

        var second = Configuration(secret: null).GetOrCreateJwtSecret();

        second.Should().Be(first);
    }

    [Fact]
    public void A_configured_secret_wins_and_no_file_is_made()
    {
        var secret = Configuration(secret: "configured").GetOrCreateJwtSecret();

        secret.Should().Be("configured");
        File.Exists(KeyFile).Should().BeFalse();
    }

    [Fact]
    public void Without_a_secret_and_a_key_file_path_it_fails()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var act = () => configuration.GetOrCreateJwtSecret();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SecretFile is required*");
    }

    [Fact]
    public void A_host_without_a_secret_signs_with_the_saved_key()
    {
        using var factory = new NoJwtSecretApiFactory();

        var secret = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.Secret;
        var keyFile = factory.Services.GetRequiredService<IConfiguration>()["Jwt:SecretFile"]!;

        keyFile.Should().StartWith(factory.Paths.Root);
        secret.Should().Be(File.ReadAllText(keyFile));
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
