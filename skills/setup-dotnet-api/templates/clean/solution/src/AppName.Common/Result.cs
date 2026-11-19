namespace AppName.Common;

public class Result
{
    protected Result(bool successful, Error error)
    {
        Successful = successful;
        Error = error;
    }

    public bool Successful { get; }

    public Error Error { get; }

    public static Result Success()
        => new(true, null!);

    public static Result Failure(Error error)
        => new(false, error);

    public static Result Failure(string error, string message)
        => Failure(new Error(error, message));

    public static Result<TValue> Success<TValue>(TValue value)
        => new(value, true, null!);

    public static Result<TValue> Failure<TValue>(Error error)
        => new(default!, false, error);

    protected static Result<TValue> Create<TValue>(TValue? value, Error error) where TValue : class
        => value is null ? Failure<TValue>(error) : Success(value);

    public static implicit operator Result(Error error) => Failure(error);

    public static explicit operator bool(Result result) => result.Successful;
}

public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool successful, Error error)
        : base(successful, error)
    {
        _value = value;
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    public TValue Value => Successful
        ? _value
        : throw new InvalidOperationException("Cannot access value when result is not successful");
}

public static class ResultExtensions
{
    public static Result<TValue> Ensure<TValue>(this Result<TValue> result, Func<TValue, bool> predicate, Error error)
    {
        if (!result.Successful)
        {
            return result;
        }

        return result.Successful && predicate(result.Value) ? result : Result.Failure<TValue>(error);
    }

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func)
        => result.Successful ? func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result> Bind<TIn>(this Result<TIn> result, Func<TIn, Task<Result>> func) =>
        result.Successful ? await func(result.Value) : Result.Failure(result.Error);

    public static async Task<Result<TOut>>
        Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func) =>
        result.Successful ? await func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<T> Match<T>(this Task<Result> resultTask, Func<T> onSuccess, Func<Error, T> onFailure)
    {
        var result = await resultTask;

        return result.Successful ? onSuccess() : onFailure(result.Error);
    }

    public static T Match<TValue, T>(this Result<TValue> result, Func<TValue, T> onSuccess, Func<Error, T> onFailure)
        => result.Successful ? onSuccess(result.Value) : onFailure(result.Error);

    public static T Match<T>(this Result result, Func<T> onSuccess, Func<Error, T> onFailure)
        => result.Successful ? onSuccess() : onFailure(result.Error);

    public static async Task<TOut> Match<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        var result = await resultTask;

        return result.Successful ? onSuccess(result.Value) : onFailure(result.Error);
    }
}