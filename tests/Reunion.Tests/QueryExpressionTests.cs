namespace Reunion.Tests;

public sealed class QueryExpressionTests
{
    [Fact]
    public void TypedResult_QueryExpression_ProjectsSuccessfulValues()
    {
        var result =
            from left in Result.Success<int, string>(20)
            from right in Result.Success<int, string>(22)
            select left + right;

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TypedResult_QueryExpression_PropagatesFailureWithoutLaterCallbacks()
    {
        var invoked = false;

        var result =
            from left in Result.Failure<int, string>("error")
            from right in Next(left)
            select left + right;

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("error", error);
        Assert.False(invoked);
        return;

        Result<int, string> Next(int value)
        {
            invoked = true;
            return Result.Success<int, string>(value);
        }
    }

    [Fact]
    public void TypedResult_QueryExpression_DoesNotProjectAnIntermediateFailure()
    {
        var projected = false;

        var result = Result<int, string>.Success(20).SelectMany(
            _ => Result<int, string>.Failure("error"),
            (left, right) =>
            {
                projected = true;
                return left + right;
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("error", error);
        Assert.False(projected);
    }

    [Fact]
    public void ResultQueryOperatorsPreserveUninitializedGuards()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Select(static value => value));
        Assert.Throws<InvalidOperationException>(() => result.SelectMany(
            static value => Result<int, string>.Success(value),
            static (left, right) => left + right));
    }

    [Fact]
    public void StringErrorResult_QueryExpression_ProjectsSuccessfulValues()
    {
        var result =
            from left in Result.Success(20)
            from right in Result.Success(22)
            select left + right;

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Option_QueryExpression_ProjectsPresentValues()
    {
        var option =
            from left in Option.Some(20)
            from right in Option.Some(22)
            select left + right;

        Assert.True(option.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Option_QueryExpression_PropagatesNoneWithoutLaterCallbacks()
    {
        var invoked = false;

        var option =
            from left in Option.None<int>()
            from right in Next(left)
            select left + right;

        Assert.True(option.IsNone);
        Assert.False(invoked);
        return;

        Option<int> Next(int value)
        {
            invoked = true;
            return Option.Some(value);
        }
    }
}
