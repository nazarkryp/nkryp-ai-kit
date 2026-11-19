using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppName.Api.Infrastructure.Configurations;
using AppName.Api.Infrastructure.ExceptionHandlers;
using AppName.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

namespace AppName.Api.Infrastructure.Hosting;

/// <summary>
/// The host setup that every API of the solution shares. <c>Program.cs</c> of a service calls
/// <see cref="AddApiDefaults"/> first, <see cref="UseApiDefaults"/> after <c>Build()</c>, then its own
/// middleware (authentication, rate limiter), and <see cref="MapApiDefaults"/> last.
/// </summary>
public static class ApiHostExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder, string[] args)
    {
        // ── Configurations ────────────────────────────────────────────────────────
        builder.ConfigureAppConfiguration(args);

        // ── Logging ───────────────────────────────────────────────────────────────
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

        // ── Errors, JSON ──────────────────────────────────────────────────────────
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.ConfigureHttpJsonOptions(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

        builder.Services.AddHttpContextAccessor();

        // ── Options ───────────────────────────────────────────────────────────────
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

        // ── CORS ──────────────────────────────────────────────────────────────────
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

        // ── Rate Limiting ─────────────────────────────────────────────────────────
        // The forwarded headers middleware runs first and trusts only the configured proxies (ForwardedHeaders
        // settings), so the partition is the client address and not the reverse proxy. A service adds its
        // own policies with another AddRateLimiter call.
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
        });

        // ── Health Checks ─────────────────────────────────────────────────────────
        builder.Services.AddHealthChecks();

        return builder;
    }

    /// <summary>
    /// The first part of the middleware pipeline, up to <c>UseCors</c>. The order matters.
    /// </summary>
    public static WebApplication UseApiDefaults(this WebApplication app)
    {
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

        return app;
    }

    /// <summary>
    /// Maps the endpoints of the service, the OpenAPI documents with the Scalar page, and the health check.
    /// </summary>
    /// <param name="title">The API name on the Scalar page, for example "Shop Admin API".</param>
    /// <param name="configureScalar">Changes the Scalar page of the service.</param>
    public static WebApplication MapApiDefaults(
        this WebApplication app,
        string title,
        Action<ScalarOptions>? configureScalar = null)
    {
        app.UseEndpoints();
        app.UseOpenApiDocumentation(title, configureScalar);

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

        return app;
    }
}
