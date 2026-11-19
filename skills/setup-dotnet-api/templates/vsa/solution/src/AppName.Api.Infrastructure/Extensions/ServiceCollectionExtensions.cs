using Asp.Versioning;
using AppName.Api.Infrastructure.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace AppName.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <param name="configure">Adds the transformers of the service, for example the Bearer security scheme.</param>
    public static IServiceCollection AddOpenApiWithVersioning(
        this IServiceCollection services,
        Action<OpenApiOptions>? configure = null)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat           = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddEndpointsApiExplorer();

        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer<ExampleSchemaTransformer>();
            configure?.Invoke(options);
        });

        return services;
    }

    /// <param name="title">The API name in the OpenAPI documents and on the Scalar page.</param>
    /// <param name="configure">Adds the Scalar settings of the service, for example the Bearer authentication.</param>
    public static void UseOpenApiDocumentation(
        this WebApplication app,
        string title,
        Action<ScalarOptions>? configure = null)
    {
        app.MapOpenApi().AllowAnonymous();

        var documents = app
            .DescribeApiVersions()
            .Select(e => new ScalarDocument(
                $"v{e.ApiVersion.MajorVersion}",
                $"{title} v{e.ApiVersion}",
                $"/openapi/v{e.ApiVersion.MajorVersion}.json"));

        app.MapScalarApiReference("/docs", options =>
        {
            options.Layout   = ScalarLayout.Classic;
            options.DarkMode = true;
            options.Theme    = ScalarTheme.DeepSpace;
            options.Title    = title;
            options.AddDocuments(documents);
            options.ShowSidebar = true;

            options
                .ExpandAllTags()
                .HideModels()
                .HideClientButton()
                .ShowOperationId();

            configure?.Invoke(options);
        }).AllowAnonymous();
    }
}

