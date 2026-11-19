// @file-module:auth
namespace AppName.ServiceName.Api.Infrastructure.Configurations;

/// <summary>
/// The limit on password guesses: requests to the routes that check a password, per client IP address.
/// </summary>
public sealed class CredentialsRateLimitSettings
{
    public const string SectionName = "CredentialsRateLimit";

    public int PermitLimit { get; init; } = 10;

    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
}
