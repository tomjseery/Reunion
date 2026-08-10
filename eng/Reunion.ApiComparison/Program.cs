using Reunion.ApiComparison;

var exact = args.Length is 3 && args[0] is "--exact";
if (args.Length is not 2 && !exact)
{
    Console.Error.WriteLine(
        "Usage: Reunion.ApiComparison [--exact] <downlevel assembly> <union assembly>");
    return 2;
}

var firstAssembly = exact ? args[1] : args[0];
var secondAssembly = exact ? args[2] : args[1];
using var downlevel = LoadedAssembly.Open("Reunion-downlevel", Path.GetFullPath(firstAssembly));
using var union = LoadedAssembly.Open("Reunion-union", Path.GetFullPath(secondAssembly));

var errors = exact
    ? ApiComparer.CompareExact(downlevel.Assembly, union.Assembly)
    : ApiComparer.Compare(downlevel.Assembly, union.Assembly);
if (errors.Count is not 0)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine(error);
    }

    return 1;
}

Console.WriteLine(exact
    ? "Public APIs match exactly."
    : "The canonical union API and downlevel compatibility projection match.");
return 0;
