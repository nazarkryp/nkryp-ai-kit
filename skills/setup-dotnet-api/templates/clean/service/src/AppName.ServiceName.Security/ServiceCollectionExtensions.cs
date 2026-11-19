// @file-module:auth
using AppName.ServiceName.Security.Abstractions;
using AppName.ServiceName.Security.Configurations;
using AppName.ServiceName.Security.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace AppName.ServiceName.Security;

public static class ServiceCollectionExtensions
{
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
