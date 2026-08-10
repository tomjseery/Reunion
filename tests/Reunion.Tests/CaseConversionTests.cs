namespace Reunion.Tests;

public sealed class CaseConversionTests
{
    [Fact]
    public void NamedCasesConvertToEveryFunctionalFamily()
    {
        Result resultSuccess = new Success();
        Result resultFailure = new Failure<string>("error");
        Result<int> valueSuccess = new Success<int>(42);
        Result<int> valueFailure = new Failure<string>("error");
        Result<int, string> typedSuccess = new Success<int>(42);
        Result<int, string> typedFailure = new Failure<string>("error");
        UnitResult<string> unitSuccess = new Success();
        UnitResult<string> unitFailure = new Failure<string>("error");
        Option<int> some = new Some<int>(42);
        Option<int> none = new None();

        Assert.True(resultSuccess.IsSuccess);
        Assert.True(resultFailure.IsFailure);
        Assert.True(valueSuccess.TryGetValue(out var value));
        Assert.Equal(42, value);
        Assert.True(valueFailure.TryGetError(out var valueError));
        Assert.Equal("error", valueError);
        Assert.True(typedSuccess.TryGetValue(out var typedValue));
        Assert.Equal(42, typedValue);
        Assert.True(typedFailure.TryGetError(out var typedError));
        Assert.Equal("error", typedError);
        Assert.True(unitSuccess.IsSuccess);
        Assert.True(unitFailure.TryGetError(out var unitError));
        Assert.Equal("error", unitError);
        Assert.True(some.TryGetValue(out var optionValue));
        Assert.Equal(42, optionValue);
        Assert.True(none.IsNone);
    }

    [Fact]
    public void SamePayloadTypesRemainDiscriminated()
    {
        Result<string, string> success = new Success<string>("same");
        Result<string, string> failure = new Failure<string>("same");

        Assert.True(success.TryGetValue(out var value));
        Assert.Equal("same", value);
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("same", error);
    }

    [Fact]
    public void InvalidDefaultPayloadCasesCannotBypassResultValidation()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> _ = default(Success<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string, int> _ = default(Success<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<int, string> _ = default(Failure<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            UnitResult<string> _ = default(Failure<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            Option<string> _ = default(Some<string>);
        });
    }

}
