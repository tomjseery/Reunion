using System.Diagnostics.CodeAnalysis;

namespace Reunion;

/// <summary>Represents success or a string error without a success value.</summary>
public readonly partial struct Result : IEquatable<Result>
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;
    internal const string SuccessText = "Success";
    internal const string FailureText = "Failure";
    internal const string UninitializedText = "Uninitialized";

    private readonly byte tag;
    private readonly string? error;

    private Result(byte tag, string? error)
    {
        this.tag = tag;
        this.error = error;
    }

    /// <summary>Gets whether the result represents success.</summary>
    public bool IsSuccess
    {
        get
        {
            this.EnsureInitialized();
            return this.tag == SuccessTag;
        }
    }

    /// <summary>Gets whether the result represents failure.</summary>
    public bool IsFailure
    {
        get
        {
            this.EnsureInitialized();
            return this.tag == FailureTag;
        }
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(SuccessTag, default);

    /// <summary>Creates a failed result.</summary>
    public static Result Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result(FailureTag, error);
    }

    /// <summary>Creates a successful result.</summary>
    public static Result<TValue> Success<TValue>(TValue value)
        where TValue : notnull =>
        Result<TValue>.Success(value);

    /// <summary>Creates a failed result.</summary>
    public static Result<TValue> Failure<TValue>(string error)
        where TValue : notnull =>
        Result<TValue>.Failure(error);

    /// <summary>Creates a successful result.</summary>
    public static Result<TValue, TError> Success<TValue, TError>(TValue value)
        where TValue : notnull
        where TError : notnull =>
        Result<TValue, TError>.Success(value);

    /// <summary>Creates a failed result.</summary>
    public static Result<TValue, TError> Failure<TValue, TError>(TError error)
        where TValue : notnull
        where TError : notnull =>
        Result<TValue, TError>.Failure(error);

    /// <summary>Invokes the callback for the active case.</summary>
    public TResult Match<TResult>(Func<TResult> success, Func<string, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag == SuccessTag ? success() : failure(this.error!);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action success, Action<string> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag == SuccessTag)
            success();
        else
            failure(this.error!);
    }

    /// <summary>Attempts to retrieve the failure error.</summary>
    public bool TryGetError([NotNullWhen(true)] out string? error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag == FailureTag;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result Bind(Func<Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Failure(this.error!);

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TValue> Bind<TValue>(Func<Result<TValue>> bind)
        where TValue : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Failure<TValue>(this.error!);

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public UnitResult<TError> Bind<TError>(
        Func<UnitResult<TError>> bind,
        Func<string, TError> mapError)
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag == FailureTag)
            return UnitResult.Failure(mapError(this.error!));

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TValue, TError> Bind<TValue, TError>(
        Func<Result<TValue, TError>> bind,
        Func<string, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag == FailureTag)
            return Failure<TValue, TError>(mapError(this.error!));

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Transforms the failure error while preserving success.</summary>
    public UnitResult<TError> MapError<TError>(Func<string, TError> map)
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? UnitResult.Success<TError>()
            : UnitResult.Failure(map(this.error!));
    }

    /// <summary>Observes a success without changing it.</summary>
    public Result Tap(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action();

        return this;
    }

    /// <summary>Observes a failure without changing it.</summary>
    public Result TapError(Action<string> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action(this.error!);

        return this;
    }

    /// <summary>Recovers from a failure.</summary>
    public Result Recover(Action<string> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        fallback(this.error!);
        return Success();
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public Result RecoverWith(Func<string, Result> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        var result = fallback(this.error!);
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Determines whether this value equals another value.</summary>
    public bool Equals(Result other) =>
        this.tag == other.tag
        && (this.tag != FailureTag || this.error == other.error);

    /// <summary>Determines whether this value equals another value.</summary>
    public override bool Equals(object? obj) => obj is Result other && this.Equals(other);

    /// <summary>Returns the hash code for this value.</summary>
    public override int GetHashCode() =>
        this.tag == FailureTag
            ? HashCode.Combine(this.tag, this.error)
            : HashCode.Combine(this.tag);

    /// <summary>Returns a string representation of this value.</summary>
    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => SuccessText,
            FailureTag => $"{FailureText}({this.error})",
            _ => UninitializedText
        };

    /// <summary>Determines whether two values are equal.</summary>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>Determines whether two values are not equal.</summary>
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");

        if (this.tag == FailureTag && string.IsNullOrWhiteSpace(this.error))
            throw new InvalidOperationException("The Result failure has no error.");
    }
}

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
            return this.tag == SuccessTag;
        }
    }

    /// <summary>Gets whether the result represents failure.</summary>
    public bool IsFailure
    {
        get
        {
            this.EnsureInitialized();
            return this.tag == FailureTag;
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

        return this.tag == SuccessTag ? success(this.value!) : failure(this.error!);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action<TValue> success, Action<TError> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag == SuccessTag)
            success(this.value!);
        else
            failure(this.error!);
    }

    /// <summary>Attempts to retrieve the successful value.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        this.EnsureInitialized();
        value = this.value;
        return this.tag == SuccessTag;
    }

    /// <summary>Attempts to retrieve the failure error.</summary>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag == FailureTag;
    }

    /// <summary>Transforms a successful value.</summary>
    public Result<TNext, TError> Map<TNext>(Func<TValue, TNext> map)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
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

        if (this.tag == FailureTag)
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

        if (this.tag == FailureTag)
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

        if (this.tag == FailureTag)
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

        if (this.tag == FailureTag)
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

        return this.tag == SuccessTag
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

        if (this.tag == FailureTag || predicate(this.value!))
            return this;

        return Failure(errorFactory());
    }

    /// <summary>Observes a success without changing it.</summary>
    public Result<TValue, TError> Tap(Action<TValue> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action(this.value!);

        return this;
    }

    /// <summary>Observes a failure without changing it.</summary>
    public Result<TValue, TError> TapError(Action<TError> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action(this.error!);

        return this;
    }

    /// <summary>Recovers from a failure.</summary>
    public Result<TValue, TError> Recover(Func<TError, TValue> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return this.tag == SuccessTag ? this : Success(fallback(this.error!));
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public Result<TValue, TError> RecoverWith(
        Func<TError, Result<TValue, TError>> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
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

        if (this.tag == SuccessTag && this.value is null)
            throw new InvalidOperationException("The Result success has no value.");

        if (this.tag == FailureTag && this.error is null)
            throw new InvalidOperationException("The Result failure has no error.");
    }
}
