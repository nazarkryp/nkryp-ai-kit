// @file-module:auth
using AppName.Api.Infrastructure.Security.Abstractions;
using AppName.Api.Infrastructure.Security.Configurations;
using AppName.Api.Infrastructure.Security.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace AppName.Api.Infrastructure.Security;

public static class ServiceCollectionExtensions
{
    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    private const int MinimumSecretBytes = 32;

    public static IServiceCollection AddAppSecurity(this IServiceCollection services, Action<JwtOptions> configure)
    {
        services
            .AddOptions<JwtOptions>()
            .Configure(configure)
            .Validate(x => !string.IsNullOrEmpty(x.KeyId), "Jwt:KeyId is required")
            .Validate(x => !string.IsNullOrEmpty(x.Audience), "Jwt:Audience is required")
            .Validate(
                x => DecodeSecret(x.Secret) is { Length: >= MinimumSecretBytes },
                $"Jwt:Secret must be base64 that decodes to at least {MinimumSecretBytes} bytes. Set it with the Jwt__Secret environment variable, for example to the output of 'openssl rand -base64 32'.")
            .Validate(x => !string.IsNullOrEmpty(x.Issuer), "Jwt:Issuer is required")
            .Validate(x => x.AccessTokenExpiresIn != default, "Jwt:AccessTokenExpiresIn is required")
            .Validate(x => x.RefreshTokenExpiresIn != default, "Jwt:RefreshTokenExpiresIn is required")
            .Validate(x => x.RefreshTokenExpiresIn >= x.AccessTokenExpiresIn, "RefreshToken must exceed AccessToken Lifetime")
            .ValidateOnStart();

        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<ITokenManager, TokenManager>();
        // @module:permissions
        services.AddSingleton<IAccountSetupCodeProvider, AccountSetupCodeProvider>();
        // @end
        services.AddScoped<IPasswordManager, PasswordManager>();

        return services;
    }

    /// <returns>The key bytes, or null when the secret is missing or is not base64.</returns>
    private static byte[]? DecodeSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return null;
        }

        var buffer = new byte[secret.Length];

        return Convert.TryFromBase64String(secret, buffer, out var written) ? buffer[..written] : null;
    }
}
