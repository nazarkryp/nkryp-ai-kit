// @file-module:auth
namespace AppName.Api.Infrastructure.Security.Identity;

public class IdentityUser
{
    public required Guid UserId { get; init; }

    public required string Username { get; init; } = null!;

    public required string EmailAddress { get; init; } = null!;
}