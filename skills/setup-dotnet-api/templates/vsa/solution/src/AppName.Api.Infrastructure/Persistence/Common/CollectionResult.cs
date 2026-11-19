namespace AppName.Api.Infrastructure.Persistence.Common;

public sealed class CollectionResult<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = [];

    public string? Next { get; set; }
}