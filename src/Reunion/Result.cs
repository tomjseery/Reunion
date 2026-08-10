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
    public static Result Success() => new(SuccessTag, default);

    /// <summary>Creates a failed result.</summary>
    public static Result Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result(FailureTag, error);
    }

    /// <summary>Converts a string error to a failed result.</summary>
    public static implicit operator Result(string error) => Failure(error);

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

        return this.tag is SuccessTag ? success() : failure(this.error!);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action success, Action<string> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag is SuccessTag)
            success();
        else
            failure(this.error!);
    }

    /// <summary>Attempts to retrieve the failure error.</summary>
    public bool TryGetError([NotNullWhen(true)] out string? error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag is FailureTag;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result Bind(Func<Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
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

        if (this.tag is FailureTag)
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

        if (this.tag is FailureTag)
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

        if (this.tag is FailureTag)
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

        return this.tag is SuccessTag
            ? UnitResult.Success<TError>()
            : UnitResult.Failure(map(this.error!));
    }

    /// <summary>Observes a success without changing it.</summary>
    public Result Tap(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is SuccessTag)
            action();

        return this;
    }

    /// <summary>Observes a failure without changing it.</summary>
    public Result TapError(Action<string> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is FailureTag)
            action(this.error!);

        return this;
    }

    /// <summary>Recovers from a failure.</summary>
    public Result Recover(Action<string> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag is SuccessTag)
            return this;

        fallback(this.error!);
        return Success();
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public Result RecoverWith(Func<string, Result> fallback)
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
    public bool Equals(Result other) =>
        this.tag == other.tag
        && (this.tag is not FailureTag || this.error == other.error);

    /// <summary>Determines whether this value equals another value.</summary>
    public override bool Equals(object? obj) => obj is Result other && this.Equals(other);

    /// <summary>Returns the hash code for this value.</summary>
    public override int GetHashCode() =>
        this.tag is FailureTag
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

        if (this.tag is FailureTag && string.IsNullOrWhiteSpace(this.error))
            throw new InvalidOperationException("The Result failure has no error.");
    }
}
