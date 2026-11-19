// @file-module:jwt
namespace AppName.ServiceName.Api.Infrastructure.Authorization;

/// <summary>
/// Endpoint metadata: any signed-in account may call the route, with no permission. The coverage test
/// accepts a route only when it has a permission, this marker, or one of the other rules.
/// </summary>
public sealed class AnySignedInUserMetadata
{
    public static readonly AnySignedInUserMetadata Instance = new();

    private AnySignedInUserMetadata()
    {
    }
}
