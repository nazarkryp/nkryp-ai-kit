namespace AppName.Api.Infrastructure.Persistence.Abstractions;

public sealed class CursorSortMismatchException(string message) : Exception(message);

