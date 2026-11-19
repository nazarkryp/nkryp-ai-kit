// @file-module:auth
namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>An account created for a test, together with its tokens.</summary>
/// <param name="RefreshToken">The value of the refresh token cookie, as the API set it.</param>
public sealed record TestAccount(
    string Username,
    string EmailAddress,
    string Password,
    string AccessToken,
    string RefreshToken);
