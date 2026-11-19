namespace AppName.Common;

public class Error(string name, string message)
{
    public Error(string name, string message, object? details)
        : this(name, message)
    {
        Message = message;
        Details = details;
    }

    public string Name { get; } = name;

    public string Message { get; } = message;

    public object? Details { get; }

    public static implicit operator string(Error error)
        => error.Name;

    public virtual Error WithMessage(string message) => new(Name, message, Details);
}

public static class ErrorExtensions
{
    extension<TError>(TError error) where TError : Error
    {
        public TError WithMessage(string message) => (TError)error.WithMessage(message);
    }
}
