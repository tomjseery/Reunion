using System.Reflection;

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
    public void RawPayloadsConvertToTheirUnambiguousCases()
    {
        Result resultFailure = "error";
        Result<int> valueSuccess = 42;
        Result<int> valueFailure = "error";
        Result<int, string> typedSuccess = 42;
        Result<int, string> typedFailure = "error";
        Result<int, TestError> inheritedFailure = new TestError.Expired();
        UnitResult<TestError> unitFailure = new TestError.Expired();
        Option<int> some = 42;

        Assert.True(resultFailure.IsFailure);
        Assert.True(valueSuccess.IsSuccess);
        Assert.True(valueFailure.IsFailure);
        Assert.True(typedSuccess.IsSuccess);
        Assert.True(typedFailure.IsFailure);
        Assert.True(inheritedFailure.TryGetError(out var inheritedError));
        Assert.IsType<TestError.Expired>(inheritedError);
        Assert.True(unitFailure.TryGetError(out var unitError));
        Assert.IsType<TestError.Expired>(unitError);
        Assert.True(some.IsSome);
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

    [Fact]
    public void PublicConversionSurfaceContainsRawPayloadsAndNamedCaseCompatibility()
    {
#if NET11_0_OR_GREATER
        AssertConversionSources(typeof(Result), typeof(string));
        AssertConversionSources(typeof(Result<int>), typeof(int), typeof(string));
        AssertConversionSources(typeof(Result<int, string>), typeof(int), typeof(string));
        AssertConversionSources(typeof(UnitResult<string>), typeof(string));
        AssertConversionSources(typeof(Option<int>), typeof(int));
#else
        AssertConversionSources(
            typeof(Result),
            typeof(string),
            typeof(Success),
            typeof(Failure<string>));
        AssertConversionSources(
            typeof(Result<int>),
            typeof(int),
            typeof(string),
            typeof(Success<int>),
            typeof(Failure<string>));
        AssertConversionSources(
            typeof(Result<int, string>),
            typeof(int),
            typeof(string),
            typeof(Success<int>),
            typeof(Failure<string>));
        AssertConversionSources(
            typeof(UnitResult<string>),
            typeof(string),
            typeof(Success),
            typeof(Failure<string>));
        AssertConversionSources(
            typeof(Option<int>),
            typeof(int),
            typeof(Some<int>),
            typeof(None));
#endif
    }

    private static void AssertConversionSources(Type target, params Type[] expectedSources)
    {
        var conversions = target
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.Equal(expectedSources.Length, conversions.Length);
        Assert.All(conversions, conversion => Assert.Equal(target, conversion.ReturnType));
        Assert.True(
            expectedSources.ToHashSet().SetEquals(
                conversions.Select(conversion =>
                    conversion.GetParameters().Single().ParameterType)));
    }

    private abstract record TestError
    {
        public sealed record Expired : TestError;
    }
}
