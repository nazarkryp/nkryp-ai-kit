// @file-module:auth
namespace AppName.Api.Infrastructure.Security.Tokens;

public sealed class CreateTokenParameters
{
    public Guid UserId { get; init; }

    public string Username { get; init; } = null!;

    public string EmailAddress { get; init; } = null!;
    // @module:permissions

    /// <summary>An admin token carries the Admin role and no permission claims.</summary>
    public bool IsAdmin { get; init; }

    /// <summary>The permission codes of an account that is not an admin.</summary>
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
    // @end
}
