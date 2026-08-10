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

Require(Match(valid) == "valid", "Native Valid matching failed.");
Require(Match(invalid) == "Name is required.", "Native Invalid matching failed.");
Require(Match(rawInvalid) == "Name is required.", "Raw validation error conversion failed.");
Require(MatchDefault(default) == "uninitialized", "Default did not match the null union state.");

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
