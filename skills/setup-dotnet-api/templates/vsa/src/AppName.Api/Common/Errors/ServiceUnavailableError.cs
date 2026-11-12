using AppName.Common;

namespace AppName.Api.Common.Errors;

public sealed class ServiceUnavailableError(string name, string message) : Error(name, message)
{
    public override Error WithMessage(string message) => new ServiceUnavailableError(Name, message);
}
