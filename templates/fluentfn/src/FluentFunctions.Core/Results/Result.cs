namespace FluentFunctions.Core.Results;

/// <summary>
/// The outcome of a handler: a value, or one or more <see cref="Error"/>s. Handlers return this
/// instead of throwing for failures they expect, so the adapters can map them without try/catch.
/// </summary>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly IReadOnlyList<Error>? _errors;

    private Result(T value)
    {
        _value = value;
        _errors = null;
    }

    private Result(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failed result needs at least one error.", nameof(errors));
        }

        _value = default;
        _errors = errors;
    }

    public bool IsSuccess => _errors is null;

    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public IReadOnlyList<Error> Errors => _errors ?? [];

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(IReadOnlyList<Error> errors) => new(errors);

    public static Result<T> Failure(Error error) => new([error]);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(_errors!);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        IsSuccess ? bind(_value!) : Result<TOut>.Failure(_errors!);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_errors!);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>
    /// Runs every check and collects all the errors, so a caller sees every problem with its
    /// input at once rather than one per attempt.
    /// </summary>
    public static Result<T> Validate<T>(T value, params IEnumerable<Func<T, Error?>> checks)
    {
        var errors = checks.Select(check => check(value)).OfType<Error>().ToList();
        return errors.Count == 0 ? Result<T>.Success(value) : Result<T>.Failure(errors);
    }
}
