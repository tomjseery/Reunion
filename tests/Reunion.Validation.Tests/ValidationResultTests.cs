using System.Reflection;
using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultTests
{
    [Fact]
    public void FactoriesAndNamedCases_CreateSelectedCases()
    {
        var errors = CreateErrors(("name", "Required."));
        ValidationResult validCase = new Valid();
        ValidationResult invalidCase = new Invalid(errors);
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(errors);

        Assert.True(valid.IsValid);
        Assert.False(valid.IsInvalid);
        Assert.False(valid.TryGetErrors(out _));
        Assert.False(invalid.IsValid);
        Assert.True(invalid.IsInvalid);
        Assert.True(invalid.TryGetErrors(out var actual));
        Assert.Same(errors, actual);
        Assert.Equal(valid, validCase);
        Assert.Equal(invalid, invalidCase);
    }

    [Fact]
    public void InvalidConstruction_RejectsNullAndDefaultCaseCannotBypassValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new Invalid(null!));
        Assert.Throws<ArgumentNullException>(() => ValidationResult.Invalid(null!));
        Assert.Throws<ArgumentNullException>(() =>
        {
            ValidationResult _ = default(Invalid);
        });
    }

    [Fact]
    public void StatePropertiesAndOperations_RejectUninitializedDefault()
    {
        var validation = default(ValidationResult);
        var operations = new Action[]
        {
            () => _ = validation.IsValid,
            () => _ = validation.IsInvalid,
            () => validation.Match(() => 1, _ => 0),
            () => validation.Match(() => { }, _ => { }),
            () => validation.TryGetErrors(out _),
            () => validation.Combine(ValidationResult.Valid()),
            () => validation.ToResult(),
            () => validation.TryGetFailure(out _)
        };

        foreach (var operation in operations)
            Assert.Throws<InvalidOperationException>(operation);
    }

    [Fact]
    public void Match_EachCase_InvokesOnlySelectedCallbackOnce()
    {
        var errors = CreateErrors(("name", "Required."));
        var validCalls = 0;
        var invalidCalls = 0;
        Func<int> valid = () => ++validCalls;
        Func<ValidationErrors, int> invalid = actual =>
        {
            Assert.Same(errors, actual);
            return ++invalidCalls;
        };

        Assert.Equal(1, ValidationResult.Valid().Match(valid, invalid));
        Assert.Equal(1, ValidationResult.Invalid(errors).Match(valid, invalid));

        ValidationResult.Valid().Match(() => validCalls++, _ => invalidCalls++);
        ValidationResult.Invalid(errors).Match(() => validCalls++, _ => invalidCalls++);

        Assert.Equal(2, validCalls);
        Assert.Equal(2, invalidCalls);
    }

    [Fact]
    public void Match_NullCallbacksAndSelectedExceptions_ArePreserved()
    {
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(CreateErrors(("name", "Required.")));
        var expected = new TestException();

        Assert.Throws<ArgumentNullException>(() => valid.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => valid.Match(() => 0, null!));
        Assert.Throws<ArgumentNullException>(() => valid.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => valid.Match(() => { }, null!));
        Assert.Same(expected, Assert.Throws<TestException>(
            () => valid.Match<int>(() => throw expected, _ => 0)));
        Assert.Same(expected, Assert.Throws<TestException>(
            () => invalid.Match<int>(() => 0, _ => throw expected)));
    }

    [Fact]
    public void EqualityHashingOperatorsAndFormatting_AreCaseAwareAndStable()
    {
        var errors = CreateErrors(("name", "Required."));
        var sameErrors = CreateErrors(("name", "Required."));
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(errors);
        var sameInvalid = ValidationResult.Invalid(sameErrors);

        Assert.Equal(valid, ValidationResult.Valid());
        Assert.Equal(invalid, sameInvalid);
        Assert.Equal(invalid.GetHashCode(), sameInvalid.GetHashCode());
        Assert.True(valid == ValidationResult.Valid());
        Assert.True(valid != invalid);
        Assert.Equal("Valid", valid.ToString());
        Assert.Equal($"Invalid({errors})", invalid.ToString());
        Assert.Equal("Uninitialized", default(ValidationResult).ToString());
        Assert.Equal(new Valid(), default(Valid));
        Assert.Equal(new Invalid(errors), new Invalid(sameErrors));
        Assert.Equal("Valid", new Valid().ToString());
        Assert.Equal($"Invalid({errors})", new Invalid(errors).ToString());
    }

    [Fact]
    public void PublicSurface_HidesPayloadAndRawConversions()
    {
        var type = typeof(ValidationResult);
        var conversions = type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Errors");
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
#if NET11_0_OR_GREATER
        Assert.Empty(conversions);
#else
        Assert.Equal([typeof(Invalid), typeof(Valid)], conversions
            .Select(method => method.GetParameters().Single().ParameterType)
            .OrderBy(candidate => candidate.Name));
#endif
    }

    internal static ValidationErrors CreateErrors(params (string Field, string Message)[] errors) =>
        new(errors.Select(error => new KeyValuePair<string, string>(error.Field, error.Message)));

    private sealed class TestException : Exception;
}
