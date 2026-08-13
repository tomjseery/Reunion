namespace Reunion.Tests;

public sealed class InterfaceSuccessConversionTests
{
    [Fact]
    public async Task InterfaceTypedValueConvertsThroughInferredSuccessCase()
    {
        Result<IReadOnlyList<ApplicationDto>, ApplicationError> result =
            (await MapAsync([new WebApplicationDto(42)])).ToSuccess();

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, Assert.Single(value).Id);
    }

    [Fact]
    public void AbstractTypedValueConvertsThroughInferredSuccessCase()
    {
        ApplicationDto value = new WebApplicationDto(42);

        Result<ApplicationDto, ApplicationError> result = value.ToSuccess();

        Assert.True(result.TryGetValue(out var resultValue));
        Assert.Same(value, resultValue);
    }

    private static Task<IReadOnlyList<ApplicationDto>> MapAsync(
        IReadOnlyList<ApplicationDto> applications) =>
        Task.FromResult(applications);

    private abstract record ApplicationDto(int Id);

    private sealed record WebApplicationDto(int Id) : ApplicationDto(Id);

    private abstract record ApplicationError;
}
