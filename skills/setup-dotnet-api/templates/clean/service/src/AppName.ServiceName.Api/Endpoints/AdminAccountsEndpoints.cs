// @file-module:permissions
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.Api.Infrastructure;
using AppName.Api.Infrastructure.Endpoints;
using AppName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using AppName.ServiceName.Application.Accounts.Commands.CreateManagedAccount;
using AppName.ServiceName.Application.Accounts.Commands.IssueAccountSetupCode;
using AppName.ServiceName.Application.Accounts.Commands.RevokeAccountSessions;
using AppName.ServiceName.Application.Accounts.Commands.SetAccountPermissions;
using AppName.ServiceName.Application.Accounts.Commands.SetAccountRole;
using AppName.ServiceName.Application.Accounts.Queries.GetManagedAccount;
using AppName.ServiceName.Application.Accounts.Queries.GetManagedAccounts;
using AppName.ServiceName.Application.Common;
using AppName.Common;
using AppName.ServiceName.Domain.Accounts;
using AppName.Api.Authentication.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Endpoints;

/// <summary>Admin endpoints that manage accounts, their roles, grants, sessions and setup codes.</summary>
public sealed class AdminAccountsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        var versionSet = builder.CreateDefaultApiVersionSet();

        var group = builder.MapGroup("v{v:apiVersion}/admin/accounts")
            .WithApiVersionSet(versionSet)
            .WithTags("Account management");

        group.MapGet(string.Empty, GetAccountsAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CollectionResponse<ManagedAccountResponse>>()
            .WithSummary("List accounts")
            .WithDescription("Returns every account with its role, grants and effective permissions.");

        group.MapGet("{accountId:guid}", GetAccountAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Get account")
            .WithDescription("Returns one account with its role, grants and effective permissions.");

        group.MapPost(string.Empty, CreateAccountAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<CreatedManagedAccountResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Create account")
            .WithDescription("Creates an account without a usable password. The response is the only place that shows the setup code. The user enters this code to set a password.");

        group.MapPut("{accountId:guid}/role", SetRoleAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Set account role")
            .WithDescription("Gives the account a new role, or no role. An admin cannot change their own role. The last admin cannot lose the Admin role. The change reaches the account at its next token refresh.");

        group.MapPut("{accountId:guid}/permissions", SetPermissionsAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Set account permissions")
            .WithDescription("Replaces the permissions granted to the account directly. The change reaches the account at its next token refresh.");

        group.MapPost("{accountId:guid}/sessions/revoke", RevokeSessionsAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<ManagedAccountResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Revoke account sessions")
            .WithDescription("Ends every session of the account. Its access tokens work until they expire, within one minute.");

        group.MapPost("{accountId:guid}/setup-code", IssueSetupCodeAsync)
            .RequirePermission(Permissions.AccountsManage)
            .Produces<AccountSetupCodeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithSummary("Issue account setup code")
            .WithDescription("Gives the account a new setup code. The new code replaces the old one. The response is the only place that shows the code.");
    }

    private static async Task<IResult> GetAccountsAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetManagedAccountsQuery(), cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> GetAccountAsync(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetManagedAccountQuery(accountId), cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateManagedAccountRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateManagedAccountCommand(
            context.User.GetUser().UserId,
            request.Username,
            request.EmailAddress,
            request.RoleId,
            request.Permissions ?? []);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            created => Results.Created($"/v1/admin/accounts/{created.Account.Id}", created),
            error => error.ToErrorResponse());
    }

    private static async Task<IResult> SetRoleAsync(
        Guid accountId,
        SetAccountRoleRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetAccountRoleCommand(context.User.GetUser().UserId, accountId, request.RoleId),
            cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> SetPermissionsAsync(
        Guid accountId,
        SetAccountPermissionsRequest request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetAccountPermissionsCommand(context.User.GetUser().UserId, accountId, request.Permissions),
            cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> RevokeSessionsAsync(
        Guid accountId,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RevokeAccountSessionsCommand(context.User.GetUser().UserId, accountId),
            cancellationToken);

        return result.ToResponse();
    }

    private static async Task<IResult> IssueSetupCodeAsync(
        Guid accountId,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new IssueAccountSetupCodeCommand(context.User.GetUser().UserId, accountId),
            cancellationToken);

        return result.ToResponse();
    }
}
