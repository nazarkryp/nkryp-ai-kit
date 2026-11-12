using FluentValidation;
using AppName.Application.Errors;
using AppName.Common;
using MediatR;
using System.Reflection;

namespace AppName.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next(cancellationToken);
        }

        var error = ValidationError.FromFailures(failures);

        if (TryCreateFailureResult(error, out var result))
        {
            return result;
        }

        throw new ValidationException(failures);
    }

    /// <summary>
    /// Returns the validation error as a failed <see cref="Result"/> or <see cref="Result{T}"/>, so the
    /// handler result maps to 400. Another response type gets a <see cref="ValidationException"/>.
    /// </summary>
    private static bool TryCreateFailureResult(ValidationError error, out TResponse result)
    {
        result = default!;

        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            result = (TResponse)(object)Result.Failure(error);

            return true;
        }

        if (!responseType.IsGenericType ||
            responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return false;
        }

        var valueType = responseType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(Result.Failure) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters() is [{ ParameterType: var parameterType }] &&
                parameterType == typeof(Error));

        result = (TResponse)failureMethod
            .MakeGenericMethod(valueType)
            .Invoke(null, [error])!;

        return true;
    }
}
