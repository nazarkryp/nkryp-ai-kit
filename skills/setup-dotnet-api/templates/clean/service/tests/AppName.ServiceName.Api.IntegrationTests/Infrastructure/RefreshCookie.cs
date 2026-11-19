// @file-module:auth
namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>Reads the refresh token cookie from a response. The API never sends the refresh token in the body.</summary>
public static class RefreshCookie
{
    public const string Name = "refresh_token";

    /// <summary>The Set-Cookie header of the refresh token, or null when the response does not set it.</summary>
    public static string? SetCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(x => x.StartsWith($"{Name}=", StringComparison.Ordinal))
            : null;

    /// <summary>The cookie value. Send it back as it is: the API encoded it for the cookie header.</summary>
    public static string ValueOf(HttpResponseMessage response)
    {
        var header = SetCookieHeader(response)
            ?? throw new InvalidOperationException($"The response has no {Name} cookie.");

        var value = header[(Name.Length + 1)..];
        var end = value.IndexOf(';');

        return end < 0 ? value : value[..end];
    }
}
