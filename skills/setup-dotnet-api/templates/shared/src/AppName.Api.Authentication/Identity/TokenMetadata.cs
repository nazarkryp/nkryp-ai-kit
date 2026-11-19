// @file-module:jwt
namespace AppName.Api.Authentication.Identity;

public class TokenMetadata
{
    public required string TokenId { get; init; } = null!;
    
    public required DateTime IssuedAt { get; init; }
    
    public required DateTime ExpiresOn { get; init; }
}