namespace AppName.Common.Errors;

/// <param name="errors">The messages of each property that failed validation.</param>
public sealed class ValidationError(string name, string message, Dictionary<string, string[]>? errors = null)
    : Error(name, message, errors)
{
    public Dictionary<string, string[]>? Errors { get; } = errors;

    public override Error WithMessage(string message) => new ValidationError(Name, message, Errors);
}
