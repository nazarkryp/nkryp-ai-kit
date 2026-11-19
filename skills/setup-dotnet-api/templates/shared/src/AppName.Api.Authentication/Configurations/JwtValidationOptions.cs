// @file-module:jwt
namespace AppName.Api.Authentication.Configurations;

/// <summary>
/// The values that every service of the solution checks an access token with. They are the same in every
/// service, so a service accepts the tokens of the auth service. <c>AddJwtAuthentication</c> validates
/// them at start.
/// </summary>
public sealed class JwtValidationOptions
{
    /// <summary>The base64 signing key: <c>Jwt:Secret</c>, or the key in <c>Jwt:SecretFile</c>.</summary>
    public string Secret { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;
}
