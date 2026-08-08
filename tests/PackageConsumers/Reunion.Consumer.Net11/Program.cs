using Reunion;

Result<int, string> success = new Success<int>(42);
Result<int, string> failure = new Failure<string>("error");
Result<string, string> sameTypeSuccess = new Success<string>("same");
Result<string, string> sameTypeFailure = new Failure<string>("same");
Option<int> some = new Some<int>(42);
Option<int> none = new None();
Option<int> defaultOption = default;

Require(Match(success) == "success:42", "Exhaustive success matching failed.");
Require(Match(failure) == "failure:error", "Exhaustive failure matching failed.");
Require(MatchSameType(sameTypeSuccess) == "success:same", "Same-type success matching failed.");
Require(MatchSameType(sameTypeFailure) == "failure:same", "Same-type failure matching failed.");
Require(MatchOption(some) == "some:42", "Some matching failed.");
Require(MatchOption(none) == "none", "Native None conversion failed.");
Require(MatchOption(defaultOption) == "none", "A default Option must match None.");

Require(success.TryGetValue(out var resultValue) && resultValue == 42, "Result.TryGetValue(out var) was ambiguous or incorrect.");
Require(some.TryGetValue(out var optionValue) && optionValue == 42, "Option.TryGetValue(out var) was ambiguous or incorrect.");

Console.WriteLine("Reunion net11 package consumer passed.");

static string Match(Result<int, string> result) => result switch
{
    Success<int> value => $"success:{value.Value}",
    Failure<string> error => $"failure:{error.Error}"
};

static string MatchSameType(Result<string, string> result) => result switch
{
    Success<string> value => $"success:{value.Value}",
    Failure<string> error => $"failure:{error.Error}"
};

static string MatchOption(Option<int> option) => option switch
{
    Some<int> value => $"some:{value.Value}",
    None _ => "none"
};

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
