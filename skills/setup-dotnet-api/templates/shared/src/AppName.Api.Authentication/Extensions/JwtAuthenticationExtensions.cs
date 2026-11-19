// @file-module:jwt
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AppName.Api.Authentication.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppName.Api.Authentication.Extensions;

public static class JwtAuthenticationExtensions
{
    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    private const int MinimumSecretBytes = 32;

    /// <summary>
    /// Checks the access tokens of the solution: HS256 with <paramref name="jwtSecret"/>, the issuer and
    /// the audience of <c>Jwt:Issuer</c> and <c>Jwt:Audience</c>. Every service uses the same values, so
    /// a service accepts the tokens of the auth service. The values are validated at start, so a bad key
    /// stops the server before it serves a request.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string jwtSecret)
    {
        services
            .AddOptions<JwtValidationOptions>()
            .Configure(options =>
            {
                options.Secret = jwtSecret;
                options.Issuer = configuration["Jwt:Issuer"]!;
                options.Audience = configuration["Jwt:Audience"]!;
            })
            .Validate(
                x => DecodeSecret(x.Secret) is { Length: >= MinimumSecretBytes },
                $"Jwt:Secret must be base64 that decodes to at least {MinimumSecretBytes} bytes. Set it with the Jwt__Secret environment variable, for example to the output of 'openssl rand -base64 32'.")
            .Validate(x => !string.IsNullOrEmpty(x.Issuer), "Jwt:Issuer is required")
            .Validate(x => !string.IsNullOrEmpty(x.Audience), "Jwt:Audience is required")
            .ValidateOnStart();

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // The validated values: reading Value runs the checks above.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtValidationOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.Secret)),
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    ClockSkew = TimeSpan.Zero,
                    // Accept only HS256 access tokens. A JWT with another algorithm or another "typ" is refused.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidTypes = [AccountTokenDefaults.AccessTokenType],
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = ClaimTypes.Role,
                    ValidateIssuer = true,
                    ValidateAudience = true
                };
            });

        services
            .AddAuthorizationBuilder()
            // Default-deny: an endpoint without its own rule needs a signed-in user. Every route also has its
            // own rule (AuthorizationEndpointExtensions), and a test checks that.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

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
