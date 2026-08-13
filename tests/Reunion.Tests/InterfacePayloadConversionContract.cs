using Reunion;

internal static class InterfacePayloadConversionContract
{
    private static async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> ConvertAsync()
    {
        return await MapAsync();
    }

    private static Task<IReadOnlyList<ApplicationDto>> MapAsync() =>
        Task.FromResult<IReadOnlyList<ApplicationDto>>([new ApplicationDto(42)]);

    private sealed record ApplicationDto(int Id);

    private abstract record ApplicationError;
}
