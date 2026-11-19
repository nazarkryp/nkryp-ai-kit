// @file-module:jwt
namespace AppName.Api.Authentication.Identity;

public class IdentityUser
{
    public required Guid UserId { get; init; }

    public required string Username { get; init; } = null!;

    public required string EmailAddress { get; init; } = null!;
}