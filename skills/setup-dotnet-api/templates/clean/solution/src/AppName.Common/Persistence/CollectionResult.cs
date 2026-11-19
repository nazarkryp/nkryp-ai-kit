namespace AppName.Common.Persistence;

public sealed class CollectionResult<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = [];

    public string? Next { get; set; }
}