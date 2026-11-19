// @file-module:auth
using System.Security.Claims;
using System.Security.Cryptography;
using AppName.Api.Authentication;
using AppName.ServiceName.Api.Infrastructure.Security.Abstractions;
// @module:permissions
using AppName.ServiceName.Api.Infrastructure.Security.Authentication;
// @end
using AppName.ServiceName.Api.Infrastructure.Security.Configurations;
using AppName.ServiceName.Api.Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AppName.ServiceName.Api.Infrastructure.Security.Impl;

public sealed class TokenProvider(IOptions<JwtOptions> jwtOptions) : ITokenProvider
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public Token GenerateToken(CreateTokenParameters parameters)
    {
        var symmetricSecurityKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.Value.Secret))
        {
            KeyId = _jwtOptions.KeyId
        };

        var credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var issuedAt = DateTime.UtcNow;
        var expiresOn = issuedAt.Add(_jwtOptions.AccessTokenExpiresIn);
        var refreshTokenExpiresOn = issuedAt.Add(_jwtOptions.RefreshTokenExpiresIn);
        var expiresIn = (int)_jwtOptions.AccessTokenExpiresIn.TotalSeconds;
        var jti = Guid.CreateVersion7().ToString();
        
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, parameters.UserId.ToString()),
            new(JwtRegisteredClaimNames.Name, parameters.Username),
            new(JwtRegisteredClaimNames.Email, parameters.EmailAddress),
            new(JwtRegisteredClaimNames.Jti, jti)
        ];

        // @module:permissions
        if (parameters.IsAdmin)
        {
            // An admin has every permission, also the ones added later, so its token names the role.
            claims.Add(new Claim(ClaimTypes.Role, AccountRoles.Admin));
        }
        else
        {
            claims.AddRange(parameters.Permissions.Select(code => new Claim(AccountTokenDefaults.PermissionClaimType, code)));
        }
        // @end

        var descriptor = new SecurityTokenDescriptor
        {
            TokenType = AccountTokenDefaults.AccessTokenType,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt,
            Expires = expiresOn,
            NotBefore = issuedAt,
            SigningCredentials = credentials,
            Audience = _jwtOptions.Audience,
            Issuer = _jwtOptions.Issuer,
        };

        var handler = new JsonWebTokenHandler();

        var accessToken = handler.CreateToken(descriptor);
        var refreshToken = GenerateRefreshToken();

        return new Token
        {
            Jti = jti,
            TokenType = _jwtOptions.TokenType,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresOn = expiresOn,
            RefreshTokenExpiresOn = refreshTokenExpiresOn,
            IssuedAt = issuedAt,
            ExpiresIn = expiresIn
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomNumber)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}