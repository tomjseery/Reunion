namespace Reunion.Tests;

public sealed class NamedCaseExtensionTests
{
    [Fact]
    public void PayloadExtensionsInferNamedCaseTypes()
    {
        Success<string> success = "value".ToSuccess();
        Failure<string> failure = "error".ToFailure();
        Some<string> some = "value".ToSome();

        Assert.Equal("value", success.Value);
        Assert.Equal("error", failure.Error);
        Assert.Equal("value", some.Value);
    }

    [Fact]
    public void PayloadExtensionsPreserveDeclaredAbstractions()
    {
        IReadOnlyList<int> values = [42];
        TestError error = new TestError.Expired();

        Success<IReadOnlyList<int>> success = values.ToSuccess();
        Failure<TestError> failure = error.ToFailure();
        Some<IReadOnlyList<int>> some = values.ToSome();

        Assert.Same(values, success.Value);
        Assert.Same(error, failure.Error);
        Assert.Same(values, some.Value);
    }

    [Fact]
    public void InferredNamedCasesConvertToTheirFamilies()
    {
        TestError error = new TestError.Expired();

        Result<string> valueSuccess = "value".ToSuccess();
        Result<int> valueFailure = "error".ToFailure();
        Result<string, TestError> typedSuccess = "value".ToSuccess();
        Result<int, TestError> typedFailure = error.ToFailure();
        UnitResult<TestError> unitFailure = error.ToFailure();
        Option<string> some = "value".ToSome();

        Assert.True(valueSuccess.IsSuccess);
        Assert.True(valueFailure.IsFailure);
        Assert.True(typedSuccess.IsSuccess);
        Assert.True(typedFailure.IsFailure);
        Assert.True(unitFailure.IsFailure);
        Assert.True(some.IsSome);
    }

    [Fact]
    public void InvalidPayloadsUseNamedCaseValidation()
    {
        string? value = null;

        Assert.Throws<ArgumentNullException>(() => value!.ToSuccess());
        Assert.Throws<ArgumentNullException>(() => value!.ToSome());
        Assert.Throws<ArgumentNullException>(() => value!.ToFailure());
        Assert.Throws<ArgumentException>(() => " ".ToFailure());
    }

    [Fact]
    public void ExtensionsAreUnambiguousForExistingLibraryValues()
    {
        var result = Result.Success<int, string>(42);
        var option = Option.Some(42);
        var success = new Success<int>(42);
        var failure = new Failure<string>("error");

        Success<Result<int, string>> resultSuccess = result.ToSuccess();
        Some<Option<int>> optionSome = option.ToSome();
        Success<Success<int>> successSuccess = success.ToSuccess();
        Failure<Failure<string>> failureFailure = failure.ToFailure();

        Assert.Equal(result, resultSuccess.Value);
        Assert.Equal(option, optionSome.Value);
        Assert.Equal(success, successSuccess.Value);
        Assert.Equal(failure, failureFailure.Error);
    }

    private abstract record TestError
    {
        public sealed record Expired : TestError;
    }
}
