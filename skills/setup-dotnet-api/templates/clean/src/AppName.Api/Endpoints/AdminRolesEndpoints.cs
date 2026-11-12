// @file-module:permissions
using AppName.Api.Contracts.Roles;
using AppName.Api.Infrastructure;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.Application.Common;
using AppName.Application.Roles.Commands.CreateRole;
using AppName.Application.Roles.Commands.DeleteRole;
using AppName.Application.Roles.Commands.UpdateRole;
using AppName.Application.Roles.Queries.GetPermissionCatalog;
using AppName.Application.Roles.Queries.GetRole;
using AppName.Application.Roles.Queries.GetRoles;
using AppName.Common;
using AppName.Domain.Accounts;
using AppName.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.Api.Endpoints;

/// <summary>Admin endpoints that manage roles, and the catalog of the permissions that a role can have.</summary>
public sealed class AdminRolesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        var versionSet = builder.CreateDefaultApiVersionSet();

        var group = builder.MapGroup("v{v:apiVersion}/admin/roles")
            .WithApiVersionSet(versionSet)
            .WithTags("Account management");

        group.MapGet(string.Empty, GetRolesAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CollectionResponse<RoleResponse>>()
            .WithSummary("List roles")
            .WithDescription("Returns every role with its permissions and the number of accounts that have it.");

        group.MapGet("{roleId:guid}", GetRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<RoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Get role")
            .WithDescription("Returns one role with its permissions and the number of accounts that have it.");

        group.MapPost(string.Empty, CreateRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Create role")
            .WithDescription("Creates a role with a unique name and a set of grantable permissions.");

        group.MapPut("{roleId:guid}", UpdateRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<RoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Update role")
            .WithDescription("Renames the role and replaces its permissions. Nobody can change the Admin role. The change reaches the accounts of the role at their next token refresh.");

        group.MapDelete("{roleId:guid}", DeleteRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Delete role")
            .WithDescription("Deletes a role that no account has. Nobody can delete the Admin role.");

        var permissionGroup = builder.MapGroup("v{v:apiVersion}/admin/permissions")
            .WithApiVersionSet(versionSet)
            .WithTags("Account management");

        permissionGroup.MapGet(string.Empty, GetPermissionCatalogAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CollectionResponse<PermissionDefinitionResponse>>()
            .WithSummary("List permissions")
            .WithDescription("Returns every permission code with its description, and if a role or a grant can have it.");
    }

    private static async Task<IResult> GetRolesAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRolesQuery(), cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> GetRoleAsync(
        Guid roleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRoleQuery(roleId), cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> CreateRoleAsync(
        SaveRoleRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateRoleCommand(context.User.GetUser().UserId, request.Name, request.Permissions),
            cancellationToken);

        return result.Match(
            role => Results.Created($"/v1/admin/roles/{role.Id}", role),
            error => error.ToErrorResponse());
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        SaveRoleRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateRoleCommand(context.User.GetUser().UserId, roleId, request.Name, request.Permissions),
            cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid roleId,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteRoleCommand(context.User.GetUser().UserId, roleId),
            cancellationToken);

        return result.Match(
            Results.NoContent,
            error => error.ToErrorResponse());
    }

    private static async Task<IResult> GetPermissionCatalogAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPermissionCatalogQuery(), cancellationToken);

        return result.ToResponse();
    }
}
