// @file-module:trust
namespace AppName.ServiceName.Api.Features.Me;

/// <summary>The signed-in user, as the access token of the auth service names it.</summary>
public sealed record MeResponse(Guid UserId, string Username, string EmailAddress);
