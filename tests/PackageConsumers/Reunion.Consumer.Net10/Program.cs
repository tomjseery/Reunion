using Reunion;

var successCase = new Success<int>(42);
var failureCase = new Failure<string>("error");
var someCase = new Some<int>(42);
var noneCase = new None();

var success = Result.Success<int, string>(successCase.Value);
var failure = Result.Failure<int, string>(failureCase.Error);
var some = Option.Some(someCase.Value);
var none = Option.None<int>();
var query =
    from left in Result<int, string>.Success(20)
    from right in Result<int, string>.Success(22)
    select left + right;

Require(success.TryGetValue(out var value) && value == 42, "Result success value was not preserved.");
Require(failure.TryGetError(out var error) && error == "error", "Result failure error was not preserved.");
Require(some.TryGetValue(out var optionValue) && optionValue == 42, "Option value was not preserved.");
Require(none.IsNone, "Option.None did not produce None.");
Require(noneCase.Equals(default(None)), "None must remain a value-type marker.");
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
