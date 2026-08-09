namespace Reunion.Tests;

public sealed class NamedCasesTests
{
    [Fact]
    public void PayloadCasesHaveValueSemantics()
    {
        Assert.Equal(new Success<int>(42), new Success<int>(42));
        Assert.NotEqual(new Success<int>(42), new Success<int>(43));
        Assert.Equal(new Failure<string>("error"), new Failure<string>("error"));
        Assert.NotEqual(new Failure<string>("error"), new Failure<string>("other"));
        Assert.Equal(new Some<int>(42), new Some<int>(42));
        Assert.NotEqual(new Some<int>(42), new Some<int>(43));
    }

    [Fact]
    public void MarkerCasesHaveValueSemantics()
    {
        Assert.Equal(new Success(), new Success());
        Assert.Equal(new None(), new None());
        Assert.True(new Success() == default);
        Assert.True(new None() == default);
    }

    [Fact]
    public void PayloadCasesDeconstructTheirPayloads()
    {
        new Success<int>(42).Deconstruct(out var success);
        new Failure<string>("error").Deconstruct(out var failure);
        new Some<int>(43).Deconstruct(out var some);

        Assert.Equal(42, success);
        Assert.Equal("error", failure);
        Assert.Equal(43, some);
    }

    [Fact]
    public void CasesHaveReadableStringRepresentations()
    {
        Assert.Equal("Success", new Success().ToString());
        Assert.Equal("Success(42)", new Success<int>(42).ToString());
        Assert.Equal("Failure(error)", new Failure<string>("error").ToString());
        Assert.Equal("Some(42)", new Some<int>(42).ToString());
        Assert.Equal("None", new None().ToString());
    }
}
