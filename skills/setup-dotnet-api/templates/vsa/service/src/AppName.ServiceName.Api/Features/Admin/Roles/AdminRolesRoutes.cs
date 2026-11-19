// @file-module:permissions
using AppName.Api.Infrastructure;

namespace AppName.ServiceName.Api.Features.Admin.Roles;

/// <summary>
/// The route groups of the admin slices that manage roles, and of the catalog of the permissions that a
/// role can have.
/// </summary>
public static class AdminRolesRoutes
{
    public static RouteGroupBuilder MapAdminRolesGroup(this IEndpointRouteBuilder builder) =>
        builder.MapGroup("v{v:apiVersion}/admin/roles")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Account management");

    public static RouteGroupBuilder MapAdminPermissionsGroup(this IEndpointRouteBuilder builder) =>
        builder.MapGroup("v{v:apiVersion}/admin/permissions")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Account management");
}
