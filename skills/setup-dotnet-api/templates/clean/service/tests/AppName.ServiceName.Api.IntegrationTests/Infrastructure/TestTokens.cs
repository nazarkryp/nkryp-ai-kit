// @file-module:trust
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AppName.Api.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Makes access tokens as the auth service of the solution does: HS256, the issuer and the audience of
/// <see cref="TestConfiguration"/>, and the <c>at+jwt</c> type. A test changes one value to check that the
/// service refuses the token.
/// </summary>
public static class TestTokens
{
    public static readonly Guid UserId = Guid.Parse("0197a3c2-4b5e-7c3d-9a1b-2c3d4e5f6a7b");

    public const string Username = "trusted.user";

    public const string EmailAddress = "trusted.user@example.test";

    /// <summary>An access token that the auth service of the tests could have issued.</summary>
    public static string Create(
        string secret = TestConfiguration.JwtSecret,
        string type = AccountTokenDefaults.AccessTokenType)
    {
        var issuedAt = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            TokenType = type,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, Username),
                new Claim(JwtRegisteredClaimNames.Email, EmailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
            ]),
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = issuedAt.AddMinutes(5),
            Issuer = TestConfiguration.JwtIssuer,
            Audience = TestConfiguration.JwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(secret)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    /// <summary>A key that is not the key of the solution.</summary>
    public static string OtherSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
