// @file-module:auth
namespace AppName.ServiceName.Api.Infrastructure.Security.Tokens;

public sealed class Token
{
    public string Jti { get; init; } = null!;

    public string TokenType { get; init; } = null!;
    
    public string AccessToken { get; init; } = null!;

    public string RefreshToken { get; init; } = null!;
    
    public int ExpiresIn { get; init; }

    public DateTime AccessTokenExpiresOn { get; init; }

    public DateTime RefreshTokenExpiresOn { get; init; }

    public DateTime IssuedAt { get; init; }
}