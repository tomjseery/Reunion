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
ValidationResult rawInvalid = errors;
UnitResult<ValidationErrors> unit = invalid.ToResult();

Require(valid.IsValid, "The Valid case did not convert to a valid result.");
Require(invalid.IsInvalid, "The Invalid case did not convert to an invalid result.");
Require(rawInvalid.IsInvalid, "Raw validation errors did not convert to an invalid result.");
Require(unit.TryGetError(out var actual) && ReferenceEquals(errors, actual),
    "ToResult did not preserve structured validation errors.");
Require(invalid.TryGetFailure(out var failure), "TryGetFailure did not return a failure.");
Result<int, ValidationErrors> valueResult = failure;
Require(valueResult.IsFailure, "The named failure did not convert for an early return.");
Require(valid.Match(() => "valid", _ => "invalid") == "valid", "Portable Match failed.");

Console.WriteLine("Reunion.Validation net10 package consumer passed.");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
