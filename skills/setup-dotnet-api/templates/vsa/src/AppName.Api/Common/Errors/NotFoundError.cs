using AppName.Common;

namespace AppName.Api.Common.Errors;

public sealed class NotFoundError : Error
{
    public NotFoundError(string name, string message) : base(name, message) { }
    
    public NotFoundError(string name, string message, object? details) : base(name, message, details) { }

    public override Error WithMessage(string message) => new NotFoundError(Name, message, Details);
}

