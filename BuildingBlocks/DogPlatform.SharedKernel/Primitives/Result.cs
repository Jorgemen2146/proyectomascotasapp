namespace DogPlatform.SharedKernel.Primitives;

/// <summary>
/// Represents the outcome of an operation that may succeed or fail.
/// Eliminates the need to throw exceptions for expected failure paths.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException(
                "A successful result cannot carry an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException(
                "A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    // ── Factory methods ──────────────────────────────────────────────────────

    public static Result Success() =>
        new(true, Error.None);

    public static Result<TValue> Success<TValue>(TValue value) =>
        new(value, true, Error.None);

    public static Result Failure(Error error) =>
        new(false, error);

    public static Result<TValue> Failure<TValue>(Error error) =>
        new(default, false, error);
}

/// <summary>
/// Represents the outcome of an operation that produces a value on success.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The value produced by a successful operation.
    /// Accessing this on a failed result throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                "Cannot access the value of a failed result. Check IsSuccess before accessing Value.");

    // ── Implicit conversion from value ───────────────────────────────────────

    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);
}
