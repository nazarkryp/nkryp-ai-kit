// @file-module:auth
using System.Security.Cryptography;
using AppName.Api.Authentication.Configurations;
using AppName.Api.Authentication.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AppName.ServiceName.UnitTests.Security;

/// <summary>
/// The key rule of the token check (AddJwtAuthentication). It is the one place that checks the key, also
/// for the key that this service signs its tokens with.
/// </summary>
public sealed class JwtSecretValidationTests
{
    [Fact]
    public void AcceptsABase64KeyOf32Bytes()
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var options = Resolve(secret);

        options.Secret.Should().Be(secret);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64!")]
    public void RejectsAMissingOrMalformedKey(string? secret)
    {
        var act = () => Resolve(secret);

        act.Should().Throw<OptionsValidationException>().WithMessage("*Jwt:Secret must be base64*");
    }

    [Fact]
    public void RejectsAKeyShorterThan32Bytes()
    {
        var act = () => Resolve(Convert.ToBase64String(RandomNumberGenerator.GetBytes(31)));

        act.Should().Throw<OptionsValidationException>().WithMessage("*at least 32 bytes*");
    }

    private static JwtValidationOptions Resolve(string? secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "https://localhost",
                ["Jwt:Audience"] = "appname-api",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddJwtAuthentication(configuration, secret!);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<JwtValidationOptions>>().Value;
    }
}
