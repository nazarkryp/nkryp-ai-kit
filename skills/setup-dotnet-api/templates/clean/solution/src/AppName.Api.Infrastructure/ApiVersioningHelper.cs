using System.Collections.Concurrent;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace AppName.Api.Infrastructure;

public static class ApiVersioningHelper
{
    private static readonly ConcurrentDictionary<string, ApiVersionSet> Cache = new();

    public static ApiVersionSet CreateDefaultApiVersionSet(this IEndpointRouteBuilder builder)
        => builder.CreateApiVersionSet(1.0);

    public static ApiVersionSet CreateApiVersionSet(this IEndpointRouteBuilder builder, params double[] versions)
    {
        var apiVersions = versions.Select(v => new ApiVersion(v)).ToArray();
        var key = string.Join(",", apiVersions.OrderBy(v => v.ToString()).Select(v => v.ToString()));

        return Cache.GetOrAdd(key, _ =>
        {
            var setBuilder = builder.NewApiVersionSet();
            
            foreach (var v in apiVersions)
            {
                setBuilder.HasApiVersion(v);
            }
            
            return setBuilder.Build();
        });
    }
}

