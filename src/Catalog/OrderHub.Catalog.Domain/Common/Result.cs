namespace OrderHub.Catalog.Domain.Common;

// Instances are created through the non-generic Result factory rather than static
// members here, which keeps those factories off the generic type (avoids CA1000).
public sealed class Result<T>
{
    private readonly T? _value;

    internal Result(T value)
    {
        _value = value;
        Error = Error.None;
        IsSuccess = true;
    }

    internal Result(Error error)
    {
        _value = default;
        Error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    /// <summary>
    /// Returns the success value, or throws when the result is a failure.
    /// Callers must check <see cref="IsSuccess"/> first.
    /// </summary>
    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                $"Cannot access Value of a failed Result. Error: {Error}");

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result.Ok(map(_value!)) : Result.Fail<TOut>(Error);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({Error})";
}

// Non-generic result for void commands, and the factory for Result<T> instances.
// Hosting the generic factories here rather than on Result<T> avoids CA1000.
public sealed class Result
{
    private static readonly Result _success = new(true, Error.None);

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => _success;

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Ok<T>(T value) => new(value);

    public static Result<T> Fail<T>(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error})";
}
