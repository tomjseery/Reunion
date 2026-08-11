using Reunion;

internal static class AmbiguousConversionContracts
{
    public static void Verify()
    {
        Result<string, string> samePayload = "value";
        Result<Failure<string>> failurePayload = new Failure<string>("error");
        Result<Failure<string>, string> typedFailurePayload = new Failure<string>("error");
        Result<int, Success<int>> successError = new Success<int>(42);
        UnitResult<Success> successUnitError = new Success();
        Option<None> nonePayload = new None();
    }
}
