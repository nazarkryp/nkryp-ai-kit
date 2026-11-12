using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using AppName.Api.Infrastructure;
using AppName.Api.Infrastructure.Configurations;
using AppName.Api.Infrastructure.ExceptionHandlers;
using AppName.Api.Infrastructure.Extensions;
using AppName.Application;
using AppName.Application.Common.Behaviors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
// @module:auth
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using AppName.Api.Contracts.Accounts;
using AppName.Api.Endpoints;
using AppName.Api.Infrastructure.Middlewares;
using AppName.Application.Accounts.Configurations;
using AppName.Application.Accounts.Mapping;
using AppName.Application.Common.Mapping;
using AppName.Domain.Accounts;
using AppName.Security;
using AppName.Security.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
// @end
// @module:permissions
using AppName.Api.Contracts.Roles;
using AppName.Api.Infrastructure.Authorization;
using AppName.Application.Accounts;
using AppName.Application.Roles.Mapping;
using AppName.Security.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
// @end

var builder = WebApplication.CreateBuilder(args);
// @module:auth
var isOpenApiDocumentGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
// @end

// ── Configurations ────────────────────────────────────────────────────────────
builder.ConfigureAppConfiguration(args);
// @module:auth

// The build-time OpenAPI generator starts the app but never serves a request. It gets a throwaway
// key, so it does not write a key file.
var jwtSecret = isOpenApiDocumentGeneration
    ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    : builder.Configuration.GetOrCreateJwtSecret();
// @end

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();

builder.Host.UseSerilog((_, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        // GlobalExceptionHandler logs the exception once. The middleware would log it a second time.
        .Filter.ByExcluding(e =>
            e.Properties.TryGetValue("SourceContext", out var sc) &&
            sc.ToString().Contains("ExceptionHandlerMiddleware"))
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// ── Errors, OpenAPI, JSON ─────────────────────────────────────────────────────
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApiWithVersioning();

builder.Services.ConfigureHttpJsonOptions(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

// ── Endpoints ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddHttpContextAccessor();
// @module:auth

// ── Authentication & Authorization ────────────────────────────────────────────
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecret)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
            ValidAudience = builder.Configuration["Jwt:Audience"]!,
            ClockSkew = TimeSpan.Zero,
            // Accept only HS256 access tokens. A JWT with another algorithm or another "typ" is refused.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidTypes = [AccountTokenDefaults.AccessTokenType],
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuer = true,
            ValidateAudience = true
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    // Default-deny: an endpoint without its own rule needs a signed-in user. Every route also has its
    // own rule (AuthorizationEndpointExtensions), and a test checks that.
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
// @module:permissions

// A permission route carries PermissionAuthorizeAttribute. The handler only reads claims, so it is a singleton.
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionDeniedResultHandler>();
// @end

builder.Services.AddTransient<TokenRevocationMiddleware>();
// @end

// ── Options ───────────────────────────────────────────────────────────────────
// Every options class is validated at start, so a bad value stops the server before it serves a request.
builder.Services
    .AddOptions<CorsSettings>()
    .Bind(builder.Configuration.GetSection(CorsSettings.SectionName))
    .Validate(
        x => x.AllowedOrigins.All(origin => Uri.TryCreate(origin, UriKind.Absolute, out _)),
        "Cors:AllowedOrigins must contain absolute URLs, for example http://localhost:4200")
    .ValidateOnStart();

builder.Services
    .AddOptions<CollectionSettings>()
    .Bind(builder.Configuration.GetSection(CollectionSettings.SectionName))
    .Validate(x => x.DefaultLimit > 0, "Collection:DefaultLimit must be greater than 0")
    .Validate(x => x.MaxLimit >= x.DefaultLimit, "Collection:MaxLimit must not be less than Collection:DefaultLimit")
    .ValidateOnStart();

// Only these proxies may set the client address and scheme. A bad entry stops the server at start.
builder.Services
    .AddOptions<ForwardedHeadersSettings>()
    .Bind(builder.Configuration.GetSection(ForwardedHeadersSettings.SectionName))
    .Validate(
        x => x.KnownProxies.All(proxy => IPAddress.TryParse(proxy, out _)),
        "ForwardedHeaders:KnownProxies must contain IP addresses, for example 172.17.0.1")
    .Validate(
        x => x.KnownNetworks.All(network => System.Net.IPNetwork.TryParse(network, out _)),
        "ForwardedHeaders:KnownNetworks must contain networks in CIDR form, for example 172.16.0.0/12")
    .ValidateOnStart();
// @module:auth

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
// @end

// ── CORS ──────────────────────────────────────────────────────────────────────
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
// The forwarded headers middleware runs first and trusts only the configured proxies (ForwardedHeaders
// settings), so the partition is the client address and not the reverse proxy.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Type = "TooManyRequests",
                Title = "Too Many Requests",
                Detail = "Too many requests. Please slow down.",
                Status = StatusCodes.Status429TooManyRequests
            }
        });
    };
    // @module:auth

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
    // @end
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

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
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

// The framework default trusts only a proxy on the same host. The configured proxies come in addition.
// A client that connects directly cannot set its own address, so it cannot get a new rate-limit bucket.
var trustedProxies = app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value;

foreach (var proxy in trustedProxies.KnownProxies)
{
    forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(proxy));
}

foreach (var network in trustedProxies.KnownNetworks)
{
    forwardedHeadersOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
}

app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseStatusCodePages();
app.UseExceptionHandler();
app.UseHttpsRedirection();

// The docs redirect runs before authorization. The fallback policy also applies to requests that match
// no endpoint, so it would refuse the redirect.
if (app.Environment.IsDevelopment())
{
    app.UseRewriter(new RewriteOptions()
        .AddRedirect("^$", "docs", StatusCodes.Status307TemporaryRedirect));
}

app.UseRouting();
app.UseCors();
// @module:auth
app.UseAuthentication();
app.UseAuthorization();
// @end
app.UseRateLimiter();
// @module:auth
app.UseMiddleware<TokenRevocationMiddleware>();
// @end
app.UseEndpoints();
app.UseOpenApiDocumentation();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message
            })
        }));
    }
}).AllowAnonymous();

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
