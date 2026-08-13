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
UnitResult<ValidationErrors> unit = invalid;

Require(Match(valid) == "valid", "Native Valid matching failed.");
Require(Match(invalid) == "Name is required.", "Native Invalid matching failed.");
Require(MatchDefault(default) == "uninitialized", "Default did not match the null union state.");
Require(unit.TryGetError(out var unitErrors) && ReferenceEquals(errors, unitErrors),
    "The lossless unit conversion did not preserve errors.");
Require(ConversionOverloads.Select(valid) == "validation",
    "The unit conversion made exact overload selection ambiguous.");
Require(ConversionOverloads.Select(new Valid()) == "validation",
    "Named-case conversion became ambiguous.");
var concert = new Concert(7);
var checkout = await Result.Success<Concert, CheckoutError>(concert)
    .Bind(value => invalid.Map<Concert, CheckoutError>(
        () => value,
        mapped => new CheckoutError.Invalid(mapped)))
    .MapAsync(value => Task.FromResult(value.Id));
Require(checkout.IsFailure, "Invalid validation did not short-circuit the package pipeline.");

Console.WriteLine("Reunion.Validation net11 package consumer passed.");

static string Match(ValidationResult validation) => validation switch
{
    Valid => "valid",
    Invalid(var errors) => errors.Errors["name"][0]
};

static string MatchDefault(ValidationResult validation) => validation switch
{
    null => "uninitialized",
    Valid => "valid",
    Invalid => "invalid"
};

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
