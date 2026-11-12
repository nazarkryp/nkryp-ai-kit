// @file-module:auth
using System.Security.Cryptography;
using AppName.Api.Infrastructure.Security;
using AppName.Api.Infrastructure.Security.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AppName.UnitTests.Security;

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

    private static JwtOptions Resolve(string? secret)
    {
        var services = new ServiceCollection();

        services.AddAppSecurity(options =>
        {
            options.KeyId = "test";
            options.Audience = "appname-api";
            options.Issuer = "https://localhost";
            options.Secret = secret!;
            options.TokenType = "Bearer";
            options.AccessTokenExpiresIn = TimeSpan.FromMinutes(1);
            options.RefreshTokenExpiresIn = TimeSpan.FromDays(7);
        });

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<JwtOptions>>().Value;
    }
}
