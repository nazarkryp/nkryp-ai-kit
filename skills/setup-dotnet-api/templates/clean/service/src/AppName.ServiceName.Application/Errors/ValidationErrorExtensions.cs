using AppName.Common.Errors;
using FluentValidation.Results;

namespace AppName.ServiceName.Application.Errors;

/// <summary>
/// Builds a <see cref="ValidationError"/> from FluentValidation failures. It is here and not in
/// <c>AppName.Common</c>, so the shared projects do not depend on FluentValidation.
/// </summary>
public static class ValidationErrorExtensions
{
    extension(ValidationError)
    {
        /// <summary>
        /// Builds the error that the validation pipeline returns: the code of a single failure, or
        /// <c>ValidationFailed</c>, with the messages grouped by property.
        /// </summary>
        public static ValidationError FromFailures(IReadOnlyList<ValidationFailure> failures)
        {
            var errors = failures
                .GroupBy(failure => failure.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            var name = failures.Count == 1 && !string.IsNullOrWhiteSpace(failures[0].ErrorCode)
                ? failures[0].ErrorCode
                : "ValidationFailed";

            var message = failures.Count == 1
                ? failures[0].ErrorMessage
                : "One or more validation errors occurred";

            return new ValidationError(name, message, errors);
        }
    }
}
