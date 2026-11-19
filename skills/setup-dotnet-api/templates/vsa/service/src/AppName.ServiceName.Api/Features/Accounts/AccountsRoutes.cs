// @file-module:auth
using AppName.Api.Infrastructure;

namespace AppName.ServiceName.Api.Features.Accounts;

/// <summary>The route group of the account slices. Every slice endpoint maps its route on this group.</summary>
public static class AccountsRoutes
{
    /// <summary>The rate limit on the routes that check a password.</summary>
    public const string CredentialsRateLimitPolicy = "AccountCredentials";

    public static RouteGroupBuilder MapAccountsGroup(this IEndpointRouteBuilder builder) =>
        builder.MapGroup("v{v:apiVersion}/accounts")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Accounts");
}
