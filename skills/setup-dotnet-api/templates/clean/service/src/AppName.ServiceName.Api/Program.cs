using FluentValidation;
using AppName.Api.Infrastructure.Extensions;
using AppName.Api.Infrastructure.Hosting;
using AppName.ServiceName.Application;
using AppName.ServiceName.Application.Common.Behaviors;
using Microsoft.Extensions.Options;
// @module:jwt
using System.Reflection;
using System.Security.Cryptography;
using AppName.Api.Authentication.Configurations;
using AppName.Api.Authentication.Extensions;
using AppName.Api.Authentication.OpenApi;
using Scalar.AspNetCore;
// @end
// @module:auth
using System.Threading.RateLimiting;
using AppName.ServiceName.Api.Contracts.Accounts;
using AppName.ServiceName.Api.Endpoints;
using AppName.ServiceName.Api.Infrastructure.Configurations;
using AppName.ServiceName.Api.Infrastructure.Middlewares;
using AppName.ServiceName.Application.Accounts.Configurations;
using AppName.ServiceName.Application.Accounts.Mapping;
using AppName.ServiceName.Application.Common.Mapping;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Security;
// @end
// @module:permissions
using AppName.ServiceName.Api.Contracts.Roles;
using AppName.ServiceName.Api.Infrastructure.Authorization;
using AppName.ServiceName.Application.Accounts;
using AppName.ServiceName.Application.Roles.Mapping;
using AppName.ServiceName.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
// @end

var builder = WebApplication.CreateBuilder(args);
// @module:jwt
var isOpenApiDocumentGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
// @end

// ── Configurations, logging, errors, JSON, options, CORS, rate limiter, health checks ──
// The setup that every service of the solution shares (AppName.Api.Infrastructure/Hosting).
builder.AddApiDefaults(args);
// @module:auth

// The build-time OpenAPI generator starts the app but never serves a request. It gets a throwaway
// key, so it does not write a key file.
var jwtSecret = isOpenApiDocumentGeneration
    ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    : builder.Configuration.GetOrCreateJwtSecret();
// @end
// @module:trust

// The build-time OpenAPI generator starts the app but never serves a request. It gets a throwaway
// key, so it does not need the key file of the auth service.
var jwtSecret = isOpenApiDocumentGeneration
    ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    : builder.Configuration.GetJwtSecret();
// @end

// ── OpenAPI ───────────────────────────────────────────────────────────────────
// @module:!jwt
builder.Services.AddOpenApiWithVersioning();
// @end
// @module:jwt
builder.Services.AddOpenApiWithVersioning(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
// @end

// ── Endpoints ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpoints(typeof(Program).Assembly);
// @module:jwt

// ── Authentication & Authorization ────────────────────────────────────────────
// @module:trust
// This service has no accounts. It accepts the access tokens of the auth service of the solution: the
// same key, issuer and audience. It cannot read the revocation list of the auth service (TokenManager),
// so a signed-out access token stays valid here until it expires.
// @end
builder.Services.AddJwtAuthentication(builder.Configuration, jwtSecret);
// @end
// @module:auth

builder.Services.AddAppSecurity(options =>
{
    options.KeyId = builder.Configuration["Jwt:KeyId"]!;
    options.Audience = builder.Configuration["Jwt:Audience"]!;
    options.Secret = jwtSecret;
    options.Issuer = builder.Configuration["Jwt:Issuer"]!;
    options.TokenType = builder.Configuration["Jwt:TokenType"]!;
    options.AccessTokenExpiresIn = builder.Configuration.GetValue<TimeSpan>("Jwt:AccessTokenExpiresIn");
    options.RefreshTokenExpiresIn = builder.Configuration.GetValue<TimeSpan>("Jwt:RefreshTokenExpiresIn");
});

// The revocation list of signed-out access tokens (TokenManager). Replace it with a shared cache
// (for example Redis) when the API runs on more than one instance.
builder.Services.AddDistributedMemoryCache();
// @module:permissions

// A permission route carries PermissionAuthorizeAttribute. The handler only reads claims, so it is a singleton.
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionDeniedResultHandler>();
// @end

builder.Services.AddTransient<TokenRevocationMiddleware>();

// ── Options ───────────────────────────────────────────────────────────────────
// Every options class is validated at start, so a bad value stops the server before it serves a request.
builder.Services
    .AddOptions<AccountSettings>()
    .Bind(builder.Configuration.GetSection(AccountSettings.SectionName))
    .Validate(x => x.MinUsernameLength > 0, "AccountSettings:MinUsernameLength must be greater than 0")
    .Validate(x => x.MaxUsernameLength >= x.MinUsernameLength, "AccountSettings:MaxUsernameLength must not be less than MinUsernameLength")
    .Validate(x => IsValidRegex(x.UsernameRegexPattern), "AccountSettings:UsernameRegexPattern must be a valid regular expression")
    .Validate(x => IsValidRegex(x.EmailAddressPattern), "AccountSettings:EmailAddressPattern must be a valid regular expression")
    .ValidateOnStart();

builder.Services
    .AddOptions<PasswordSettings>()
    .Bind(builder.Configuration.GetSection(PasswordSettings.SectionName))
    .Validate(x => x.MinPasswordLength > 0, "PasswordSettings:MinPasswordLength must be greater than 0")
    .Validate(x => x.MaxPasswordLength >= x.MinPasswordLength, "PasswordSettings:MaxPasswordLength must not be less than MinPasswordLength")
    .Validate(x => x.MaxConsecutiveCharacters > 0, "PasswordSettings:MaxConsecutiveCharacters must be greater than 0")
    .ValidateOnStart();

// A bad limit would make every sign-in fail at request time, so the values are checked at start.
builder.Services
    .AddOptions<CredentialsRateLimitSettings>()
    .Bind(builder.Configuration.GetSection(CredentialsRateLimitSettings.SectionName))
    .Validate(x => x.PermitLimit > 0, "CredentialsRateLimit:PermitLimit must be greater than 0")
    .Validate(x => x.Window > TimeSpan.Zero, "CredentialsRateLimit:Window must be greater than 0")
    .ValidateOnStart();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
// AddApiDefaults sets the rejection response. This call adds the policy of the routes that check a password.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(AccountsEndpoints.CredentialsRateLimitPolicy, httpContext =>
    {
        var limit = httpContext.RequestServices.GetRequiredService<IOptions<CredentialsRateLimitSettings>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = limit.PermitLimit,
                Window = limit.Window,
                QueueLimit = 0
            });
    });
});
// @end

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    // MediatR 13+ is commercial (Lucky Penny Software). Without a key it logs a license warning at start.
    // Set the key with the MediatR__LicenseKey environment variable; never commit it.
    cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"];
    cfg.RegisterServicesFromAssembly(ApplicationAssembly.Instance);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(ApplicationAssembly.Instance, includeInternalTypes: true);

// IMapper implementations are registered here by hand. There is no assembly scan for them.
// @module:auth
// @module:!permissions
builder.Services.AddScoped<IMapper<Account, AccountResponse>, AccountResponseMapper>();
// @end
// @end
// @module:permissions
builder.Services.AddScoped<IMapper<AccountAccess, AccountResponse>, AccountResponseMapper>();
builder.Services.AddScoped<IMapper<ManagedAccount, ManagedAccountResponse>, ManagedAccountResponseMapper>();
builder.Services.AddScoped<IMapper<RoleUsage, RoleResponse>, RoleResponseMapper>();
builder.Services.AddScoped<AccountAccessResolver>();
builder.Services.AddScoped<ManagedAccountLoader>();
// @end
// @module:auth

// ── Persistence ───────────────────────────────────────────────────────────────
// [setup:persistence-services] The database step of the setup registers the repositories here.
// @end

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// The ValidateOnStart checks run here, before the app does any work. An invalid setting must stop the
// server before it changes data.
app.Services.GetRequiredService<IStartupValidator>().Validate();
// @module:auth

if (!isOpenApiDocumentGeneration)
{
    // [setup:persistence-initialize] The database step of the setup creates or migrates the schema here.
}
// @end

// ── Middleware pipeline (the order matters) ───────────────────────────────────
// Forwarded headers, errors, HTTPS redirection, the docs redirect, routing and CORS.
app.UseApiDefaults();
// @module:jwt
app.UseAuthentication();
app.UseAuthorization();
// @end
app.UseRateLimiter();
// @module:auth
app.UseMiddleware<TokenRevocationMiddleware>();
// @end

// The endpoints of this service, the OpenAPI documents with the Scalar page at /docs, and /health.
// @module:!jwt
app.MapApiDefaults("AppName ServiceName API");
// @end
// @module:jwt
app.MapApiDefaults("AppName ServiceName API", options =>
{
    options.AddHttpAuthentication("Bearer", scheme =>
    {
        scheme.WithDescription("Enter your JWT bearer token");
    });
});
// @end

app.Run();
// @module:auth

static bool IsValidRegex(string pattern)
{
    try
    {
        _ = new System.Text.RegularExpressions.Regex(pattern);
        return true;
    }
    catch (ArgumentException)
    {
        return false;
    }
}
// @end

public partial class Program;
