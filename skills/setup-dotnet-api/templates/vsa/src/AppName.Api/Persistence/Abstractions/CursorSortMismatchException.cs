namespace AppName.Api.Persistence.Abstractions;

public sealed class CursorSortMismatchException(string message) : Exception(message);

