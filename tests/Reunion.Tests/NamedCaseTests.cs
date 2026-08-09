namespace Reunion.Tests;

public sealed class NamedCaseTests
{
    [Fact]
    public void MarkerCasesCanBeConstructed()
    {
        _ = new Success();
        _ = new None();
    }

    [Fact]
    public void PayloadCasesExposeTheirPayloads()
    {
        var success = new Success<string>("value");
        var failure = new Failure<string>("error");
        var some = new Some<string>("value");

        Assert.Equal("value", success.Value);
        Assert.Equal("error", failure.Error);
        Assert.Equal("value", some.Value);
    }

    [Fact]
    public void PayloadCaseConstructorsRejectNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Success<string>(null!));
        Assert.Throws<ArgumentNullException>(() => new Failure<string>(null!));
        Assert.Throws<ArgumentNullException>(() => new Some<string>(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void StringFailureCasesRejectEmptyErrors(string error)
    {
        Assert.Throws<ArgumentException>(() => new Failure<string>(error));
    }

    [Fact]
    public void DefaultPayloadCasesRemainInvalid()
    {
        Assert.Null(default(Success<string>).Value);
        Assert.Null(default(Failure<string>).Error);
        Assert.Null(default(Some<string>).Value);
    }
}
