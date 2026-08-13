using Reunion;

var successCase = new Success<int>(42);
var failureCase = new Failure<string>("error");
var someCase = new Some<int>(42);
var noneCase = new None();

Result<int, string> success = successCase;
Result<int, string> failure = failureCase;
Result<string, string> sameTypeSuccess = new Success<string>("same");
Result<string, string> sameTypeFailure = new Failure<string>("same");
Option<int> some = someCase;
Option<int> none = noneCase;
Result resultSuccess = new Success();
Result resultFailure = failureCase;
Result<int> valueSuccess = successCase;
Result<int> valueFailure = failureCase;
UnitResult<string> unitSuccess = new Success();
UnitResult<string> unitFailure = failureCase;
Result<int> rawValueSuccess = 42;
Result<int, string> rawTypedSuccess = 42;
Result<int, string> rawTypedFailure = "raw error";
UnitResult<string> rawUnitFailure = "raw error";
var mappedUnitSuccess = unitSuccess.Map(() => 42);
var mappedUnitFailure = unitFailure.Map(() => 42);
Option<int> rawSome = 42;
Option<string> rawNone = null;
var query =
    from left in Result<int, string>.Success(20)
    from right in Result<int, string>.Success(22)
    select left + right;

Require(success.TryGetValue(out var value) && value == 42, "Result success value was not preserved.");
Require(failure.TryGetError(out var error) && error == "error", "Result failure error was not preserved.");
Require(sameTypeSuccess.IsSuccess, "Same-type success conversion was ambiguous.");
Require(sameTypeFailure.IsFailure, "Same-type failure conversion was ambiguous.");
Require(some.TryGetValue(out var optionValue) && optionValue == 42, "Option value was not preserved.");
Require(none.IsNone, "Option.None did not produce None.");
Require(noneCase.Equals(default(None)), "None must remain a value-type marker.");
Require(resultSuccess.IsSuccess && resultFailure.IsFailure, "Result case conversions failed.");
Require(valueSuccess.IsSuccess && valueFailure.IsFailure, "Result<T> case conversions failed.");
Require(unitSuccess.IsSuccess && unitFailure.IsFailure, "UnitResult case conversions failed.");
Require(rawValueSuccess.IsSuccess, "Raw Result<T> success conversion failed.");
Require(rawTypedSuccess.IsSuccess && rawTypedFailure.IsFailure, "Raw typed Result conversions failed.");
Require(rawUnitFailure.IsFailure, "Raw UnitResult error conversion failed.");
Require(mappedUnitSuccess.IsSuccess && mappedUnitFailure.IsFailure, "UnitResult.Map composition failed.");
Require(rawSome.IsSome, "Raw Option value conversion failed.");
Require(rawNone.IsNone, "Null Option conversion failed.");
Require(Result.Success().Match(() => true, _ => false), "Conventional Result.Match failed.");
Require(query.TryGetValue(out var queryValue) && queryValue == 42, "Result LINQ composition failed.");
Require(new Success<int>(42).ToString() == "Success(42)", "Named case formatting failed.");

Console.WriteLine("Reunion net10 package consumer passed.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
