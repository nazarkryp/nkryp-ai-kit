// @file-module:trust
namespace AppName.ServiceName.Api.Contracts.Me;

/// <summary>The signed-in user, as the access token of the auth service names it.</summary>
public sealed class MeResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;
}
