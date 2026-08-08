using System.Diagnostics.CodeAnalysis;

namespace Reunion;

/// <summary>Represents either a successful value or a string error.</summary>
public readonly partial struct Result<TValue> : IEquatable<Result<TValue>>
    where TValue : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TValue? value;
    private readonly string? error;

    private Result(byte tag, TValue? value, string? error)
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
    public static Result<TValue> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TValue>(SuccessTag, value, default);
    }

    /// <summary>Creates a failed result.</summary>
    public static Result<TValue> Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result<TValue>(FailureTag, default, error);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public TResult Match<TResult>(
        Func<TValue, TResult> success,
        Func<string, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag is SuccessTag ? success(this.value!) : failure(this.error!);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action<TValue> success, Action<string> failure)
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
    public bool TryGetError([NotNullWhen(true)] out string? error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag is FailureTag;
    }

    /// <summary>Transforms a successful value.</summary>
    public Result<TNext> Map<TNext>(Func<TValue, TNext> map)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag is SuccessTag
            ? Result.Success(map(this.value!))
            : Result.Failure<TNext>(this.error!);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TNext> Bind<TNext>(Func<TValue, Result<TNext>> bind)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
            return Result.Failure<TNext>(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result Bind(Func<TValue, Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
            return Result.Failure(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Transforms the failure error while preserving success.</summary>
    public Result<TValue, TError> MapError<TError>(Func<string, TError> map)
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag is SuccessTag
            ? Result.Success<TValue, TError>(this.value!)
            : Result.Failure<TValue, TError>(map(this.error!));
    }

    /// <summary>Validates a successful value against a predicate.</summary>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, Func<string> errorFactory)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (this.tag is FailureTag || predicate(this.value!))
            return this;

        return Failure(errorFactory());
    }

    /// <summary>Observes a success without changing it.</summary>
    public Result<TValue> Tap(Action<TValue> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is SuccessTag)
            action(this.value!);

        return this;
    }

    /// <summary>Observes a failure without changing it.</summary>
    public Result<TValue> TapError(Action<string> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is FailureTag)
            action(this.error!);

        return this;
    }

    /// <summary>Recovers from a failure.</summary>
    public Result<TValue> Recover(Func<string, TValue> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return this.tag is SuccessTag ? this : Success(fallback(this.error!));
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public Result<TValue> RecoverWith(Func<string, Result<TValue>> fallback)
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
    public bool Equals(Result<TValue> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag switch
        {
            SuccessTag => EqualityComparer<TValue>.Default.Equals(this.value!, other.value!),
            FailureTag => this.error == other.error,
            _ => true
        };
    }

    /// <summary>Determines whether this value equals another value.</summary>
    public override bool Equals(object? obj) =>
        obj is Result<TValue> other && this.Equals(other);

    /// <summary>Returns the hash code for this value.</summary>
    public override int GetHashCode() =>
        this.tag switch
        {
            SuccessTag => HashCode.Combine(
                this.tag,
                EqualityComparer<TValue>.Default.GetHashCode(this.value!)),
            FailureTag => HashCode.Combine(this.tag, this.error),
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
    public static bool operator ==(Result<TValue> left, Result<TValue> right) => left.Equals(right);

    /// <summary>Determines whether two values are not equal.</summary>
    public static bool operator !=(Result<TValue> left, Result<TValue> right) => !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");

        if (this.tag is SuccessTag && this.value is null)
            throw new InvalidOperationException("The Result success has no value.");

        if (this.tag is FailureTag && string.IsNullOrWhiteSpace(this.error))
            throw new InvalidOperationException("The Result failure has no error.");
    }
}
