// @file-module:auth
using System.Text.Json.Serialization;

namespace AppName.Api.Contracts.Accounts;

public sealed class TokenResponse
{
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>
    /// The new refresh token. The endpoint writes it to the HttpOnly cookie and never sends it in the body.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; init; } = null!;

    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }

    [JsonPropertyName("expires_on")]
    public required DateTimeOffset ExpiresOn { get; init; }
    // @module:permissions

    /// <summary>
    /// Whether the account has the Admin role. The client reads this value, not the access token
    /// (RFC 9068 section 6).
    /// </summary>
    [JsonPropertyName("is_admin")]
    public required bool IsAdmin { get; init; }

    /// <summary>The permission codes of the account. An admin has every code.</summary>
    [JsonPropertyName("permissions")]
    public required IReadOnlyList<string> Permissions { get; init; }
    // @end
}
