namespace AppName.Api.Infrastructure.Configurations;

/// <summary>
/// The reverse proxies whose X-Forwarded-For and X-Forwarded-Proto headers the server trusts. Without an
/// entry, only a proxy on the same host (loopback) is trusted. Then a client that connects directly cannot
/// choose its own address, for example to get a new rate-limit bucket with each request.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Single proxy addresses, for example <c>172.17.0.1</c>.</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>Proxy networks in CIDR form, for example <c>172.16.0.0/12</c> for the Docker networks.</summary>
    public string[] KnownNetworks { get; init; } = [];
}
