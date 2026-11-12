// @file-module:auth
using AppName.Security.Abstractions;
using AppName.Security.Extensions;

namespace AppName.Api.Infrastructure.Middlewares;

public sealed class TokenRevocationMiddleware(
    ITokenManager tokenManager,
    IProblemDetailsService problemDetailsService,
    ILogger<TokenRevocationMiddleware> logger)
    : IMiddleware
{
    private readonly ITokenManager _tokenManager = tokenManager;
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;
    private readonly ILogger<TokenRevocationMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            var tokenMetadata = context.User.GetTokenMetadata();

            if (!await _tokenManager.ValidateTokenAsync(tokenMetadata.TokenId))
            {
                _logger.LogWarning("Revoked token access attempt. TokenId: {TokenId}", tokenMetadata.TokenId);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers["WWW-Authenticate"] = "Bearer error=\"invalid_token\"";

                await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new()
                    {
                        Type = "Unauthorized",
                        Title = "Unauthorized",
                        Detail = "The token used has been revoked.",
                        Status = StatusCodes.Status401Unauthorized,
                        Extensions =
                        {
                            ["error"] = "token_revoked"
                        }
                    }
                });

                return;
            }
        }

        await next(context);
    }
}