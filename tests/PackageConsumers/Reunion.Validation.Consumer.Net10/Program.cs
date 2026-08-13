using Reunion;
using Reunion.Errors;
using Reunion.Validation;

var errors = new ValidationErrors(
    new Dictionary<string, string[]>
    {
        ["name"] = ["Name is required."]
    });
ValidationResult valid = new Valid();
ValidationResult invalid = new Invalid(errors);
UnitResult<ValidationErrors> unit = invalid.ToResult();
UnitResult<ValidationErrors> implicitUnit = invalid;

Require(valid.IsValid, "The Valid case did not convert to a valid result.");
Require(invalid.IsInvalid, "The Invalid case did not convert to an invalid result.");
Require(unit.TryGetError(out var actual) && ReferenceEquals(errors, actual),
    "ToResult did not preserve structured validation errors.");
Require(implicitUnit.TryGetError(out var implicitErrors) && ReferenceEquals(errors, implicitErrors),
    "The lossless unit conversion did not preserve structured validation errors.");
Require(invalid.TryGetFailure(out var failure), "TryGetFailure did not return a failure.");
Result<int, ValidationErrors> valueResult = failure;
Require(valueResult.IsFailure, "The named failure did not convert for an early return.");
Require(valid.Match(() => "valid", _ => "invalid") == "valid", "Portable Match failed.");
Require(ConversionOverloads.Select(valid) == "validation",
    "The unit conversion made exact overload selection ambiguous.");
Require(ConversionOverloads.Select(new Valid()) == "validation",
    "Named-case conversion became ambiguous.");
var concert = new Concert(7);
var checkout = await Result.Success<Concert, CheckoutError>(concert)
    .Ensure(
        _ => invalid,
        mapped => new CheckoutError.Invalid(mapped))
    .MapAsync(value => Task.FromResult(value.Id));
Require(checkout.IsFailure, "Invalid validation did not short-circuit the package pipeline.");

Console.WriteLine("Reunion.Validation net10 package consumer passed.");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed record Concert(int Id);

abstract record CheckoutError
{
    public sealed record Invalid(ValidationErrors Errors) : CheckoutError;
}

static class ConversionOverloads
{
    public static string Select(ValidationResult validation) => "validation";

    public static string Select(UnitResult<ValidationErrors> result) => "unit";
}
