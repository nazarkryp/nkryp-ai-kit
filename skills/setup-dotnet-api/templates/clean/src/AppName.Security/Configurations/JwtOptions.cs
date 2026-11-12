// @file-module:auth
namespace AppName.Security.Configurations;

public sealed class JwtOptions
{
    public string KeyId { get; set; } = null!;
    
    public string Secret { get; set; } = null!;
    
    public string Audience { get; set; } = null!;
    
    public string Issuer { get; set; } = null!;

    public string TokenType { get; set; } = null!;

    public TimeSpan AccessTokenExpiresIn { get; set; }

    public TimeSpan RefreshTokenExpiresIn { get; set; }
}