// @file-module:permissions
using AppName.Api.Contracts.Accounts;
using AppName.Api.Infrastructure.Extensions;
using AppName.Application.Errors;
using AppName.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AppName.Api.Infrastructure.Authorization;

/// <summary>
/// Writes a missing permission as the same ProblemDetails that a handler error gives:
/// <c>InsufficientPermissions</c> with the permissions that the route needs. It also logs the denial.
/// Every other result (a missing sign-in, a worker policy failure, success) goes to the default handler.
/// </summary>
public sealed class PermissionDeniedResultHandler(ILogger<PermissionDeniedResultHandler> logger)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // Forbidden is set only for a signed-in user. Without a sign-in the result is a challenge (401).
        var missing = authorizeResult.Forbidden
            ? authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<PermissionRequirement>().FirstOrDefault()
            : null;

        if (missing is null)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        logger.LogWarning(
            "SecurityEvent {Event}: account {AccountId} was denied {Method} {Path}, which needs any of {RequiredPermissions}",
            "AuthorizationDenied",
            context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            context.Request.Method,
            context.Request.Path,
            missing.AnyOf);

        await BusinessErrors.InsufficientPermissions(new InsufficientPermissionsDetails(missing.AnyOf))
            .ToErrorResponse()
            .ExecuteAsync(context);
    }
}
