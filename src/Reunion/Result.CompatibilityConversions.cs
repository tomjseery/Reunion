#if !NET11_0_OR_GREATER
namespace Reunion;

public readonly partial struct Result
{
    /// <summary>Converts a named success case to a result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator Result(Success success) => Success();

    /// <summary>Converts a named failure case to a result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator Result(Failure<string> failure) => Failure(failure.Error);
}
#endif
