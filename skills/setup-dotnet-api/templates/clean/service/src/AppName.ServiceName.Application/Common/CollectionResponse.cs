namespace AppName.ServiceName.Application.Common;

public sealed class CollectionResponse<T> where T : class
{
    public IReadOnlyList<T> Data { get; set; } = [];

    public string? Next { get; set; }

    public int Count => Data.Count;

    public int TotalCount { get; set; }
}