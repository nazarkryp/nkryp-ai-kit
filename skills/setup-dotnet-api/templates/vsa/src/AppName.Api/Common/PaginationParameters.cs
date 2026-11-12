namespace AppName.Api.Common;

/// <summary>
/// The cursor and the page size of a request that returns a dataset. A query that can sort adds its own
/// <c>OrderBy</c> value next to this record.
/// </summary>
public sealed record PaginationParameters(string? Cursor, int Limit);
