// @file-module:auth
using AppName.ServiceName.Api.Infrastructure.Security.Abstractions;
using AppName.ServiceName.Api.Infrastructure.Security.Configurations;
using AppName.ServiceName.Api.Infrastructure.Security.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace AppName.ServiceName.Api.Infrastructure.Security;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The token issuing of the auth service. AddJwtAuthentication validates the key, the issuer and the
    /// audience at start.
    /// </summary>
    public static IServiceCollection AddAppSecurity(this IServiceCollection services, Action<JwtOptions> configure)
    {
        services
            .AddOptions<JwtOptions>()
            .Configure(configure)
            .Validate(x => !string.IsNullOrEmpty(x.KeyId), "Jwt:KeyId is required")
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
}
