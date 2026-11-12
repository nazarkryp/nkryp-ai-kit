using Asp.Versioning;
using AppName.Api.Infrastructure.OpenApi;
using Scalar.AspNetCore;

namespace AppName.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiWithVersioning(this IServiceCollection services)
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
            // @module:auth
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            // @end
            options.AddSchemaTransformer<ExampleSchemaTransformer>();
        });

        return services;
    }

    public static void UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi().AllowAnonymous();

        var documents = app
            .DescribeApiVersions()
            .Select(e => new ScalarDocument(
                $"v{e.ApiVersion.MajorVersion}",
                $"AppName v{e.ApiVersion}",
                $"/openapi/v{e.ApiVersion.MajorVersion}.json"));

        app.MapScalarApiReference("/docs", options =>
        {
            options.Layout   = ScalarLayout.Classic;
            options.DarkMode = true;
            options.Theme    = ScalarTheme.DeepSpace;
            options.Title    = "AppName API";
            options.AddDocuments(documents);
            options.ShowSidebar = true;

            options
                .ExpandAllTags()
                .HideModels()
                .HideClientButton()
                .ShowOperationId();
            // @module:auth

            options.AddHttpAuthentication("Bearer", scheme =>
            {
                scheme.WithDescription("Enter your JWT bearer token");
            });
            // @end
        }).AllowAnonymous();
    }
}

