using AppName.Common;

namespace AppName.Api.Infrastructure.Common.Errors;

public sealed class ForbiddenError : Error
{
    public ForbiddenError(string name, string message) : base(name, message) { }
    
    public ForbiddenError(string name, string message, object? details) : base(name, message, details) { }

    public override Error WithMessage(string message) => new ForbiddenError(Name, message, Details);
}

