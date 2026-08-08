using System.Diagnostics.CodeAnalysis;

namespace Reunion;

/// <summary>Represents either a successful value or a typed error.</summary>
public readonly partial struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
    where TValue : notnull
    where TError : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TValue? value;
    private readonly TError? error;

    private Result(byte tag, TValue? value, TError? error)
    {
        this.tag = tag;
        this.value = value;
        this.error = error;
    }

    /// <summary>Gets whether the result represents success.</summary>
    public bool IsSuccess
    {
        get
        {
            this.EnsureInitialized();
            return this.tag is SuccessTag;
        }
    }

    /// <summary>Gets whether the result represents failure.</summary>
    public bool IsFailure
    {
        get
        {
            this.EnsureInitialized();
            return this.tag is FailureTag;
        }
    }

    /// <summary>Creates a successful result.</summary>
    public static Result<TValue, TError> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TValue, TError>(SuccessTag, value, default);
    }

    /// <summary>Creates a failed result.</summary>
    public static Result<TValue, TError> Failure(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue, TError>(FailureTag, default, error);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public TResult Match<TResult>(
        Func<TValue, TResult> success,
        Func<TError, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag is SuccessTag ? success(this.value!) : failure(this.error!);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action<TValue> success, Action<TError> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag is SuccessTag)
            success(this.value!);
        else
            failure(this.error!);
    }

    /// <summary>Attempts to retrieve the successful value.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        this.EnsureInitialized();
        value = this.value;
        return this.tag is SuccessTag;
    }

    /// <summary>Attempts to retrieve the failure error.</summary>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag is FailureTag;
    }

    /// <summary>Transforms a successful value.</summary>
    public Result<TNext, TError> Map<TNext>(Func<TValue, TNext> map)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag is SuccessTag
            ? Result.Success<TNext, TError>(map(this.value!))
            : Result.Failure<TNext, TError>(this.error!);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result Bind(
        Func<TValue, Result> bind,
        Func<TError, string> mapError)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag is FailureTag)
            return Result.Failure(mapError(this.error!));

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TNext> Bind<TNext>(
        Func<TValue, Result<TNext>> bind,
        Func<TError, string> mapError)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag is FailureTag)
            return Result.Failure<TNext>(mapError(this.error!));

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public UnitResult<TError> Bind(Func<TValue, UnitResult<TError>> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
            return UnitResult.Failure(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TNext, TError> Bind<TNext>(Func<TValue, Result<TNext, TError>> bind)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
            return Result.Failure<TNext, TError>(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Transforms the failure error while preserving success.</summary>
    public Result<TValue, TNextError> MapError<TNextError>(Func<TError, TNextError> map)
        where TNextError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag is SuccessTag
            ? Result.Success<TValue, TNextError>(this.value!)
            : Result.Failure<TValue, TNextError>(map(this.error!));
    }

    /// <summary>Validates a successful value against a predicate.</summary>
    public Result<TValue, TError> Ensure(
        Func<TValue, bool> predicate,
        Func<TError> errorFactory)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (this.tag is FailureTag || predicate(this.value!))
            return this;

        return Failure(errorFactory());
    }

    /// <summary>Observes a success without changing it.</summary>
    public Result<TValue, TError> Tap(Action<TValue> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is SuccessTag)
            action(this.value!);

        return this;
    }

    /// <summary>Observes a failure without changing it.</summary>
    public Result<TValue, TError> TapError(Action<TError> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is FailureTag)
            action(this.error!);

        return this;
    }

    /// <summary>Recovers from a failure.</summary>
    public Result<TValue, TError> Recover(Func<TError, TValue> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return this.tag is SuccessTag ? this : Success(fallback(this.error!));
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public Result<TValue, TError> RecoverWith(
        Func<TError, Result<TValue, TError>> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag is SuccessTag)
            return this;

        var result = fallback(this.error!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Determines whether this value equals another value.</summary>
    public bool Equals(Result<TValue, TError> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag switch
        {
            SuccessTag => EqualityComparer<TValue>.Default.Equals(this.value!, other.value!),
            FailureTag => EqualityComparer<TError>.Default.Equals(this.error!, other.error!),
            _ => true
        };
    }

    /// <summary>Determines whether this value equals another value.</summary>
    public override bool Equals(object? obj) =>
        obj is Result<TValue, TError> other && this.Equals(other);

    /// <summary>Returns the hash code for this value.</summary>
    public override int GetHashCode() =>
        this.tag switch
        {
            SuccessTag => HashCode.Combine(
                this.tag,
                EqualityComparer<TValue>.Default.GetHashCode(this.value!)),
            FailureTag => HashCode.Combine(
                this.tag,
                EqualityComparer<TError>.Default.GetHashCode(this.error!)),
            _ => HashCode.Combine(this.tag)
        };

    /// <summary>Returns a string representation of this value.</summary>
    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => $"{Result.SuccessText}({this.value})",
            FailureTag => $"{Result.FailureText}({this.error})",
            _ => Result.UninitializedText
        };

    /// <summary>Determines whether two values are equal.</summary>
    public static bool operator ==(
        Result<TValue, TError> left,
        Result<TValue, TError> right) =>
        left.Equals(right);

    /// <summary>Determines whether two values are not equal.</summary>
    public static bool operator !=(
        Result<TValue, TError> left,
        Result<TValue, TError> right) =>
        !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");

        if (this.tag is SuccessTag && this.value is null)
            throw new InvalidOperationException("The Result success has no value.");

        if (this.tag is FailureTag && this.error is null)
            throw new InvalidOperationException("The Result failure has no error.");
    }
}
