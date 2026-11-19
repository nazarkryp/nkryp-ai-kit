namespace AppName.Api.Infrastructure;

public sealed class CollectionSettings
{
    public const string SectionName = "Collection";

    public int DefaultLimit { get; set; } = 24;
    public int MaxLimit { get; set; } = 100;
}

