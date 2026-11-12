// @file-module:auth
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AppName.Api.Infrastructure.Security.Identity;
using AppName.Api.Infrastructure.Security.Tokens;

namespace AppName.Api.Infrastructure.Security.Extensions;

public static class IdentityExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public TokenMetadata GetTokenMetadata()
        {
            if (claimsPrincipal.Identity is not { IsAuthenticated: true })
            {
                throw new InvalidOperationException("The claims principal is not authenticated.");
            }

            var tokenId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)!.Value;
            var issuedAtUnix = long.Parse(claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Iat)!.Value);
            var expiresOnUnix = long.Parse(claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Exp)!.Value);

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix).UtcDateTime;
            var expiresOn = DateTimeOffset.FromUnixTimeSeconds(expiresOnUnix).UtcDateTime;

            return new TokenMetadata
            {
                TokenId = tokenId,
                IssuedAt = issuedAt,
                ExpiresOn = expiresOn
            };
        }
        
        public IdentityUser GetUser()
        {
            if (claimsPrincipal.Identity is not { IsAuthenticated: true })
            {
                throw new InvalidOperationException("The claims principal is not authenticated.");
            }

            var userId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;
            var username = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Name)!.Value;
            var emailAddress = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Email)!.Value;

            return new IdentityUser
            {
                UserId = Guid.Parse(userId),
                Username = username,
                EmailAddress = emailAddress
            };
        }
    }
}