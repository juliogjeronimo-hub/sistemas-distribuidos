namespace OrderHub.Catalog.Domain.Common;

/// <summary>
/// Discriminated union that represents either a successful outcome (with a value)
/// or a failure (with an <see cref="Error"/>).
/// Instances are created via the <see cref="Result"/> factory class, not through
/// static members on this generic type (avoids CA1000).
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
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
    /// Returns the success value. Throws <see cref="InvalidOperationException"/>
    /// when called on a failure result — callers must check <see cref="IsSuccess"/> first.
    /// </summary>
    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                $"Cannot access Value of a failed Result. Error: {Error}");

    /// <summary>Projects the value using <paramref name="map"/> when successful.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result.Ok(map(_value!)) : Result.Fail<TOut>(Error);

    /// <summary>Implicitly wraps a value in a successful result.</summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Implicitly wraps an error in a failed result.</summary>
    public static implicit operator Result<T>(Error error) => new(error);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({Error})";
}

/// <summary>
/// Non-generic result for commands that produce no return value,
/// and factory class for creating <see cref="Result{T}"/> instances
/// without having to specify the type on static calls (avoids CA1000).
/// </summary>
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

    // ── Non-generic (void) factories ─────────────────────────────────────────

    /// <summary>Returns a successful non-generic result.</summary>
    public static Result Success() => _success;

    /// <summary>Returns a failed non-generic result.</summary>
    public static Result Failure(Error error) => new(false, error);

    // ── Generic factories (CA1000-safe: static members on non-generic type) ──

    /// <summary>Creates a successful <see cref="Result{T}"/> wrapping <paramref name="value"/>.</summary>
    public static Result<T> Ok<T>(T value) => new(value);

    /// <summary>Creates a failed <see cref="Result{T}"/> carrying <paramref name="error"/>.</summary>
    public static Result<T> Fail<T>(Error error) => new(error);

    /// <summary>Implicitly wraps an error in a failed result.</summary>
    public static implicit operator Result(Error error) => Failure(error);

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error})";
}
