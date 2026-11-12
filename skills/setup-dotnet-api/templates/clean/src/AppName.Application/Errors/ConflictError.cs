using AppName.Common;

namespace AppName.Application.Errors;

public sealed class ConflictError : Error
{
    public ConflictError(string name, string message) : base(name, message) { }

    public ConflictError(string name, string message, object? details) : base(name, message, details) { }

    public override Error WithMessage(string message) => new ConflictError(Name, message, Details);
}

