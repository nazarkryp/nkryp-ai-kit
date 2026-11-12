// @file-module:auth
namespace AppName.Api.Infrastructure.Security.Tokens;

public class TokenMetadata
{
    public required string TokenId { get; init; } = null!;
    
    public required DateTime IssuedAt { get; init; }
    
    public required DateTime ExpiresOn { get; init; }
}