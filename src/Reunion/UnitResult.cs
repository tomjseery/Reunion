using System.Diagnostics.CodeAnalysis;

namespace Reunion;

/// <summary>Creates unit result values.</summary>
public static class UnitResult
{
    /// <summary>Creates a successful result.</summary>
    public static UnitResult<TError> Success<TError>()
        where TError : notnull =>
        UnitResult<TError>.Success();

    /// <summary>Creates a failed result.</summary>
    public static UnitResult<TError> Failure<TError>(TError error)
        where TError : notnull =>
        UnitResult<TError>.Failure(error);
}

/// <summary>Represents success or a typed error without a success value.</summary>
public readonly partial struct UnitResult<TError> : IEquatable<UnitResult<TError>>
    where TError : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TError? error;

    private UnitResult(byte tag, TError? error)
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
    public static UnitResult<TError> Success() => new(SuccessTag, default);

    /// <summary>Creates a failed result.</summary>
    public static UnitResult<TError> Failure(TError error)
    {
        ResultGuards.ThrowIfInvalidError(error, nameof(error));
        return new UnitResult<TError>(FailureTag, error);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public TResult Match<TResult>(Func<TResult> success, Func<TError, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag is SuccessTag ? success() : failure(this.error!);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action success, Action<TError> failure)
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
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag is FailureTag;
    }

    /// <summary>Creates a value when the result is successful and otherwise preserves the error.</summary>
    public Result<TValue, TError> Map<TValue>(Func<TValue> map)
        where TValue : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag is SuccessTag
            ? Result.Success<TValue, TError>(map())
            : Result.Failure<TValue, TError>(this.error!);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result Bind(Func<Result> bind, Func<TError, string> mapError)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag is FailureTag)
            return Result.Failure(mapError(this.error!));

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TValue> Bind<TValue>(
        Func<Result<TValue>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag is FailureTag)
            return Result.Failure<TValue>(mapError(this.error!));

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public UnitResult<TError> Bind(Func<UnitResult<TError>> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
            return this;

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Result<TValue, TError> Bind<TValue>(Func<Result<TValue, TError>> bind)
        where TValue : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag is FailureTag)
            return Result.Failure<TValue, TError>(this.error!);

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    /// <summary>Transforms the failure error while preserving success.</summary>
    public UnitResult<TNextError> MapError<TNextError>(Func<TError, TNextError> map)
        where TNextError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag is SuccessTag
            ? UnitResult.Success<TNextError>()
            : UnitResult.Failure(map(this.error!));
    }

    /// <summary>Observes a success without changing it.</summary>
    public UnitResult<TError> Tap(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is SuccessTag)
            action();

        return this;
    }

    /// <summary>Observes a failure without changing it.</summary>
    public UnitResult<TError> TapError(Action<TError> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag is FailureTag)
            action(this.error!);

        return this;
    }

    /// <summary>Recovers from a failure.</summary>
    public UnitResult<TError> Recover(Action<TError> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag is SuccessTag)
            return this;

        fallback(this.error!);
        return Success();
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public UnitResult<TError> RecoverWith(Func<TError, UnitResult<TError>> fallback)
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
    public bool Equals(UnitResult<TError> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag is not FailureTag
            || EqualityComparer<TError>.Default.Equals(this.error!, other.error!);
    }

    /// <summary>Determines whether this value equals another value.</summary>
    public override bool Equals(object? obj) =>
        obj is UnitResult<TError> other && this.Equals(other);

    /// <summary>Returns the hash code for this value.</summary>
    public override int GetHashCode() =>
        this.tag is FailureTag
            ? HashCode.Combine(this.tag, EqualityComparer<TError>.Default.GetHashCode(this.error!))
            : HashCode.Combine(this.tag);

    /// <summary>Returns a string representation of this value.</summary>
    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => Result.SuccessText,
            FailureTag => $"{Result.FailureText}({this.error})",
            _ => Result.UninitializedText
        };

    /// <summary>Determines whether two values are equal.</summary>
    public static bool operator ==(UnitResult<TError> left, UnitResult<TError> right) =>
        left.Equals(right);

    /// <summary>Determines whether two values are not equal.</summary>
    public static bool operator !=(UnitResult<TError> left, UnitResult<TError> right) =>
        !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The UnitResult is uninitialized.");

        if (this.tag is FailureTag)
            ResultGuards.ThrowIfStoredErrorIsInvalid(
                this.error,
                "The UnitResult failure has no error.");
    }
}
