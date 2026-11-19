// @file-module:permissions
using AppName.Api.Infrastructure;

namespace AppName.ServiceName.Api.Features.Admin.Accounts;

/// <summary>
/// The route group of the admin slices that manage accounts, their roles, grants, sessions and setup codes.
/// </summary>
public static class AdminAccountsRoutes
{
    public static RouteGroupBuilder MapAdminAccountsGroup(this IEndpointRouteBuilder builder) =>
        builder.MapGroup("v{v:apiVersion}/admin/accounts")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Account management");
}
