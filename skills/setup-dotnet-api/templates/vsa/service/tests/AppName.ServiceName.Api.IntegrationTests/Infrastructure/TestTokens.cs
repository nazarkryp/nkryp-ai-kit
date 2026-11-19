// @file-module:trust
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Signs access tokens like the auth service of the solution does: HS256, the "at+jwt" type, and the
/// issuer, audience and key that <see cref="TestConfiguration"/> gives this service.
/// </summary>
public static class TestTokens
{
    public const string Secret = "hjJlQPe9BU/DCMZj5za+vGR2rlo/afL3ezWO6ow6ig4=";

    public const string Issuer = "https://localhost";

    public const string Audience = "appname-api";

    /// <summary>The JWT "typ" header of an access token (RFC 9068).</summary>
    public const string AccessTokenType = "at+jwt";

    /// <param name="secret">The base64 signing key. Null signs with <see cref="Secret"/>.</param>
    /// <param name="type">The "typ" header of the token.</param>
    public static string Create(
        Guid userId,
        string username,
        string emailAddress,
        string? secret = null,
        string type = AccessTokenType)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(secret ?? Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var issuedAt = DateTime.UtcNow;

        var header = new JwtHeader(credentials)
        {
            [JwtHeaderParameterNames.Typ] = type
        };

        var payload = new JwtPayload(
            Issuer,
            Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, username),
                new Claim(JwtRegisteredClaimNames.Email, emailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: issuedAt,
            expires: issuedAt.AddMinutes(5),
            issuedAt: issuedAt);

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
    }

    /// <summary>A valid key that the service does not know.</summary>
    public static string CreateOtherSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
