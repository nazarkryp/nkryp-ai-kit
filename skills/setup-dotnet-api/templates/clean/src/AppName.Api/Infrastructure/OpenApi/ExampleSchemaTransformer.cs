using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppName.Api.Infrastructure.OpenApi;

public sealed class ExampleSchemaTransformer(IConfiguration configuration) : IOpenApiSchemaTransformer
{
    private readonly IReadOnlyDictionary<string, JsonElement> _examples = LoadExamples(configuration);

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var key = GetTypeKey(context.JsonTypeInfo.Type);

        if (_examples.TryGetValue(key, out var example))
        {
            schema.Example = JsonSerializer.SerializeToNode(example);
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, JsonElement> LoadExamples(IConfiguration configuration)
    {
        var examples = new Dictionary<string, JsonElement>();

        foreach (var child in configuration.GetSection("OpenApiExamples").GetChildren())
        {
            var value = SectionToObject(child);
            var json  = JsonSerializer.Serialize(value);
            examples[child.Key] = JsonSerializer.Deserialize<JsonElement>(json);
        }

        return examples;
    }

    private static object? SectionToObject(IConfigurationSection section)
    {
        var children = section.GetChildren().ToArray();

        if (children.Length == 0) return section.Value;

        if (children.All(c => int.TryParse(c.Key, out _)))
        {
            return children.Select(SectionToObject).ToList();
        }

        return children.ToDictionary(c => c.Key, SectionToObject);
    }

    private static string GetTypeKey(Type type)
    {
        if (!type.IsGenericType) return type.FullName ?? type.Name;

        var baseName = type.GetGenericTypeDefinition().FullName?.Split('`')[0];
        var args     = string.Join(", ", type.GetGenericArguments().Select(t => t.FullName ?? t.Name));
        return $"{baseName}<{args}>";
    }
}


