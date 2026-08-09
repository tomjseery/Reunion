using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultCombineTests
{
    [Fact]
    public void Combine_AllCasePairs_ReturnExpectedCase()
    {
        var leftErrors = ValidationResultTests.CreateErrors(("left", "Left."));
        var rightErrors = ValidationResultTests.CreateErrors(("right", "Right."));
        var valid = ValidationResult.Valid();
        var left = ValidationResult.Invalid(leftErrors);
        var right = ValidationResult.Invalid(rightErrors);

        Assert.Equal(valid, valid.Combine(valid));
        Assert.Equal(left, left.Combine(valid));
        Assert.Equal(right, valid.Combine(right));

        var combined = left.Combine(right);
        Assert.True(combined.TryGetErrors(out var errors));
        Assert.Equal(["Left."], errors.Errors["left"]);
        Assert.Equal(["Right."], errors.Errors["right"]);
    }

    [Fact]
    public void Combine_RepeatedFields_PreservesOrderDuplicatesAndSources()
    {
        var left = ValidationResultTests.CreateErrors(
            ("name", "Required."),
            ("name", "Duplicate."));
        var right = ValidationResultTests.CreateErrors(
            ("name", "Duplicate."),
            ("name", "Too long."),
            ("email", "Invalid."));

        var combined = ValidationResult.Invalid(left).Combine(ValidationResult.Invalid(right));

        Assert.True(combined.TryGetErrors(out var errors));
        Assert.Equal(["Required.", "Duplicate.", "Duplicate.", "Too long."], errors.Errors["name"]);
        Assert.Equal(["Invalid."], errors.Errors["email"]);
        Assert.Equal(["Required.", "Duplicate."], left.Errors["name"]);
        Assert.Equal(["Duplicate.", "Too long."], right.Errors["name"]);
        Assert.NotSame(left, errors);
        Assert.NotSame(right, errors);
    }

    [Fact]
    public void CollectionCombine_EmptyAndMixedInput_EnumeratesOnceAndAccumulatesAll()
    {
        Assert.Equal(ValidationResult.Valid(), Array.Empty<ValidationResult>().Combine());

        var enumerations = 0;
        IEnumerable<ValidationResult> Source()
        {
            enumerations++;
            yield return ValidationResult.Valid();
            yield return ValidationResult.Invalid(
                ValidationResultTests.CreateErrors(("name", "First.")));
            yield return ValidationResult.Invalid(
                ValidationResultTests.CreateErrors(("name", "Second."), ("email", "Invalid.")));
        }

        var combined = Source().Combine();

        Assert.Equal(1, enumerations);
        Assert.True(combined.TryGetErrors(out var errors));
        Assert.Equal(["First.", "Second."], errors.Errors["name"]);
        Assert.Equal(["Invalid."], errors.Errors["email"]);
        Assert.Throws<ArgumentNullException>(
            () => ValidationResultExtensions.Combine(null!));
    }
}
