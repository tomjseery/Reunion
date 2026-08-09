using Reunion.ApiComparison;

var exact = args.Length is 3 && args[0] is "--exact";
if (args.Length is not 2 && !exact)
{
    Console.Error.WriteLine(
        "Usage: Reunion.ApiComparison [--exact] <net10 assembly> <net11 assembly>");
    return 2;
}

var firstAssembly = exact ? args[1] : args[0];
var secondAssembly = exact ? args[2] : args[1];
using var net10 = LoadedAssembly.Open("Reunion-net10", Path.GetFullPath(firstAssembly));
using var net11 = LoadedAssembly.Open("Reunion-net11", Path.GetFullPath(secondAssembly));

var errors = exact
    ? ApiComparer.CompareExact(net10.Assembly, net11.Assembly)
    : ApiComparer.Compare(net10.Assembly, net11.Assembly);
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
    : "Public APIs match semantically; only net10 compatibility conversions and "
        + "net11 IUnion providers differ.");
return 0;
